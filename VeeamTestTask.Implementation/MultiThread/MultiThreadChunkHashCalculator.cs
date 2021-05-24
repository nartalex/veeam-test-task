using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    public class MultiThreadChunkHashCalculator : IChunkHashCalculator
    {
        private Exception _calculationExceptionFromConsumerThread = null;

        public void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            using var fileStream = File.OpenRead(path);
            SplitFileAndCalculateHashes(fileStream, blockSize, hashAlgorithmName, resultWriter);
        }

        public void SplitFileAndCalculateHashes(Stream fileStream, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            // Это позволяет нам не создавать слишком большой массив буффера, если файл сам по себе меньше размера блока
            blockSize = CalculateBlockSize(fileStream.Length, blockSize);

            // Расчитываем, сколько мы можем создать блоков в памяти
            var amountOfBlocks = CalculateAmountOfBlocks(blockSize);

            // Счетчик потоков поможет нам правильно завершить работу
            var allThreadsAreCompletedEvent = new AutoResetEvent(false);
            ThreadCounter.Initialize(allThreadsAreCompletedEvent);

            // Эти события будут доставлять информацию в другие потоки
            var exitByFileEndingEvent = new ManualResetEventSlim(false);
            var exitByErrorEvent = new ManualResetEventSlim(false);

            // Алгоритм работает на основе двух очередей: readyToGetMemoryBlocks (доступные для хэширования) и releasedMemoryBlocks (доступные для перезаписи)
            // Прочитанный блок файла записывается в уже созданный буфер, затем отправляется в очередь readyToGetMemoryBlocks.
            // Оттуда его подхватывает первый попавшийся consumer-поток, делает свои дела и затем ссылку на буфер отправляет в releasedMemoryBlocks, таким образом сообщая, что буфер доступен для следующего блока
            // Из этой очереди producer-поток достает свободный буфер, записывает в него блок файла и опять отправляет в очередь readyToGetMemoryBlocks
            var memoryBlockIsReleasedEvent = new ManualResetEventSlim(true);
            var releasedMemoryBlocks = new MemoryBlocksManager<byte[]>(amountOfBlocks, memoryBlockIsReleasedEvent, exitByFileEndingEvent);

            var memoryBlockIsReadyToGetEvent = new ManualResetEventSlim(true);
            var readyToGetMemoryBlocks = new MemoryBlocksManager<ReadyToGetMemoryBlock>(amountOfBlocks, memoryBlockIsReadyToGetEvent, exitByFileEndingEvent);

            var chunkIndex = 1;
            var numberOfBytes = 1;
            byte[] currentBuffer;

            try
            {
                // Первичное чтение файла, ограниченное количеством блоков amountOfBlocks
                // Здесь мы создаем буферы, создаем потоки и уже начинаем хэшировать файл
                for (; chunkIndex <= amountOfBlocks && !exitByErrorEvent.IsSet; chunkIndex++)
                {
                    currentBuffer = new byte[blockSize];

                    numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);
                    if (numberOfBytes == 0)
                    {
                        SuccessfullExit();
                        return;
                    }

                    if (blockSize > numberOfBytes)
                    {
                        currentBuffer = currentBuffer[0..numberOfBytes];
                    }

                    readyToGetMemoryBlocks.Enqueue(new ReadyToGetMemoryBlock(chunkIndex, currentBuffer));

                    var thread = new ConsumerThread(
                                    new ConsumerThreadParams(
                                        releasedMemoryBlocks: releasedMemoryBlocks,
                                        readyToGetMemoryBlocks: readyToGetMemoryBlocks,
                                        hashAlgorithmName: hashAlgorithmName,
                                        resultWriter: resultWriter,
                                        exitByFileEndingEvent: exitByFileEndingEvent,
                                        exitByErrorEvent: exitByErrorEvent,
                                        errorNotifierMethod: this.OnCalculationError
                                 ));
                }
            }
            catch (Exception e)
            {
                ForceExitCausedByThisThread(e);
                return;
            }

            if (exitByErrorEvent.IsSet)
            {
                ForceExitCausedByConsumerThread();
                return;
            }

            try
            {
                while (!exitByErrorEvent.IsSet)
                {
                    // Ждем, пока будет доступен буфер для перезаписи
                    Debug.WriteLine("Producer thread is trying to get released memory block");
                    if (!releasedMemoryBlocks.TryDequeue(out currentBuffer))
                    {
                        Debug.WriteLine("Producer thread got nothing");
                        continue;
                    }

                    Debug.WriteLine("Producer thread got free memory block");

                    numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);
                    if (numberOfBytes == 0)
                    {
                        SuccessfullExit();
                        return;
                    }

                    if (blockSize > numberOfBytes)
                    {
                        currentBuffer = currentBuffer[0..numberOfBytes];
                    }

                    Debug.WriteLine($"Producer thread is enqueueing chunk #{chunkIndex}");
                    readyToGetMemoryBlocks.Enqueue(new ReadyToGetMemoryBlock(chunkIndex, currentBuffer));

                    chunkIndex++;
                }
            }
            catch (Exception e)
            {
                ForceExitCausedByThisThread(e);
                return;
            }

            if (exitByErrorEvent.IsSet)
            {
                ForceExitCausedByConsumerThread();
                return;
            }

            return;

            /// Успешное завершение алгоритма
            void SuccessfullExit()
            {
                exitByFileEndingEvent.Set();

                allThreadsAreCompletedEvent.WaitOne();
                var shouldBeFalse = resultWriter.HasMessagesInBuffer;
                Debug.Assert(!shouldBeFalse, "Message buffer should be empty after file ending");

                DisposeEverything();
            }

            /// Аварийное завершение алгоритма из-за исключения, которое возникло в consumer-потоке
            void ForceExitCausedByConsumerThread()
            {
                Debug.WriteLine("Producer thread is shutting down after consumer thread exception");

                ForceExit();

                Console.WriteLine(_calculationExceptionFromConsumerThread);
            }

            /// Аварийное завершение алгоритма из-за исключения, которое возникло в producer-потоке
            void ForceExitCausedByThisThread(Exception e)
            {
                Debug.WriteLine("Producer thread caused exception");
                Debug.WriteLine(e);

                ForceExit();

                Console.WriteLine(e);
            }

            void ForceExit()
            {
                resultWriter.AbortOutput();
                exitByErrorEvent.Set();
                readyToGetMemoryBlocks.AbortExecution();
                releasedMemoryBlocks.AbortExecution();
                allThreadsAreCompletedEvent.WaitOne();
                DisposeEverything();
            }

            void DisposeEverything()
            {
                memoryBlockIsReleasedEvent.Dispose();
                memoryBlockIsReadyToGetEvent.Dispose();
                allThreadsAreCompletedEvent.Dispose();
                exitByFileEndingEvent.Dispose();
                exitByErrorEvent.Dispose();
            }
        }

        /// <summary>
        /// Метод, указывающий текущему потоку на ошибку в другом потоке
        /// </summary>
        /// <param name="e"></param>
        public void OnCalculationError(Exception e)
        {
            _calculationExceptionFromConsumerThread = e;
        }

        private int CalculateBlockSize(long fileLenght, int userDefinedBlockSize)
        {
            return fileLenght < userDefinedBlockSize
                    ? (int)fileLenght
                    : userDefinedBlockSize;
        }

        /// <summary>
        /// Расчитать количество блоков, которые могут быть размещены в памяти исходя из свободной ОЗУ
        /// </summary>
        /// <param name="blockSize">Размер блока</param>
        /// <returns></returns>
        private int CalculateAmountOfBlocks(int blockSize)
        {
            int amountOfBlocks = 1;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var ramCounter = new PerformanceCounter("Memory", "Available bytes");
                var availableMemory = Convert.ToUInt64(ramCounter?.NextValue());
                amountOfBlocks = (int)Math.Floor(availableMemory / (float)blockSize * 0.8);

                // Если можно создать огромное количество блоков, нам они не понадобятся
                if (amountOfBlocks > Environment.ProcessorCount)
                {
                    amountOfBlocks = Environment.ProcessorCount;
                }
            }
            // todo: Linux & MacOS support

            return amountOfBlocks;
        }
    }
}

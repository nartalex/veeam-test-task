using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    public class ProducerThreadFor3rdAttempt : IChunkHashCalculator
    {
        private bool _calculationErrorOccuredInConsumerThread = false;
        private Exception _calculationExceptionFromConsumerThread = null;

        public void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            using var fileStream = File.OpenRead(path);
            SplitFileAndCalculateHashes(fileStream, blockSize, hashAlgorithmName, resultWriter);
        }

        public void SplitFileAndCalculateHashes(Stream fileStream, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            // Это позволяет нам не создавать слишком большой массив буффера,
            // если файл сам по себе меньше размера блока
            var bytesLeft = fileStream.Length;
            if (bytesLeft < blockSize)
            {
                Debug.WriteLine($"File length is lower than block size, new block size is {bytesLeft} b");
                blockSize = (int)bytesLeft;
            }

            // Расчитываем, сколько мы можем создать блоков в памяти
            // Если вдруг программа запущена не на Windows, ограничимся количеством блоков, равным количеству ядер
            int amountOfBlocksAllowedInMemory = Environment.ProcessorCount;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var ramCounter = new PerformanceCounter("Memory", "Available bytes");
                var availableMemory = Convert.ToUInt64(ramCounter?.NextValue());
                amountOfBlocksAllowedInMemory = (int)Math.Floor(availableMemory / (float)blockSize * 0.8);

                // Если можно создать огромное количество блоков, нам они не понадобятся
                if (amountOfBlocksAllowedInMemory > Environment.ProcessorCount)
                {
                    amountOfBlocksAllowedInMemory = Environment.ProcessorCount;
                }
            }

            var allThreadsAreCompletedEvent = new AutoResetEvent(false);
            var fileHasEndedEvent = new FileHasEndedEvent();
            var calculationErrorEvent = new CalculationErrorEvent();
            ThreadCounterFor3rdAttempt.Initialize(allThreadsAreCompletedEvent);

            // Алгоритм работает на основе двух очередей: readyToGetMemoryBlocks (доступные для хэширования) releasedMemoryBlocks (уже прохешированные)

            var memoryBlockIsReleasedEvent = new ManualResetEventSlim(true);
            var releasedMemoryBlocks = new MemoryBlocksManagerFor3rdAttempt<byte[]>(amountOfBlocksAllowedInMemory, memoryBlockIsReleasedEvent);

            var memoryBlockIsReadyToGetEvent = new ManualResetEventSlim(true);
            var readyToGetMemoryBlocks = new MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock>(amountOfBlocksAllowedInMemory, memoryBlockIsReadyToGetEvent);
            fileHasEndedEvent.OnFileHasEnded += readyToGetMemoryBlocks.OnFileHasEnded;

            var chunkIndex = 1;
            var numberOfBytes = 1;
            byte[] currentBuffer;

            try
            {
                for (; chunkIndex <= amountOfBlocksAllowedInMemory && !_calculationErrorOccuredInConsumerThread; chunkIndex++)
                {
                    currentBuffer = new byte[blockSize];

                    numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);
                    if (numberOfBytes == 0)
                    {
                        WaitForThreadsAndCleanUp();
                        return;
                    }

                    if (blockSize > numberOfBytes)
                    {
                        currentBuffer = currentBuffer[0..numberOfBytes];
                    }

                    readyToGetMemoryBlocks.Enqueue(new ReadyToGetMemoryBlock(chunkIndex, currentBuffer));
                    memoryBlockIsReadyToGetEvent.Set();

                    var thread = new ConsumerThreadFor3rdAttempt(
                                    new HashCalculationThreadParamsFor3rdAttempt(
                                        releasedMemoryBlocks: releasedMemoryBlocks,
                                        readyToGetMemoryBlocks: readyToGetMemoryBlocks,
                                        hashAlgorithmName: hashAlgorithmName,
                                        resultWriter: resultWriter,
                                        calculationErrorEvent: calculationErrorEvent
                                 ));

                    fileHasEndedEvent.OnFileHasEnded += thread.OnFileHasEnded;
                    calculationErrorEvent.OnCalculationError += thread.OnCalculationError;
                }
            }
            catch (Exception e)
            {
                ForceQuitAndOutputException(e);
            }

            if (_calculationErrorOccuredInConsumerThread)
            {
                ForceQuitAndOutputExceptionFromConsumerThread();
            }

            try
            {
                while (!_calculationErrorOccuredInConsumerThread)
                {
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
                        WaitForThreadsAndCleanUp();
                        return;
                    }

                    if (blockSize > numberOfBytes)
                    {
                        currentBuffer = currentBuffer[0..numberOfBytes];
                    }

                    Debug.WriteLine($"Producer thread is enqueueing chunk #{chunkIndex}");
                    readyToGetMemoryBlocks.Enqueue(new ReadyToGetMemoryBlock(chunkIndex, currentBuffer));
                    memoryBlockIsReadyToGetEvent.Set();

                    chunkIndex++;
                }
            }
            catch (Exception e)
            {
                ForceQuitAndOutputException(e);
            }

            if (_calculationErrorOccuredInConsumerThread)
            {
                ForceQuitAndOutputExceptionFromConsumerThread();
            }

            return;

            /// Успешное завершение алгоритма
            void WaitForThreadsAndCleanUp()
            {
                fileHasEndedEvent.Fire();
                memoryBlockIsReadyToGetEvent.Set();         // Заставляем треды посмотреть в очередь еще раз

                allThreadsAreCompletedEvent.WaitOne();
                var _ = resultWriter.HasMessagesInBuffer;

                DisposeEverything();
            }

            /// Аварийное завершение алгоритма из-за исключения, которое возникло в консьюмере
            void ForceQuitAndOutputExceptionFromConsumerThread()
            {
                allThreadsAreCompletedEvent.WaitOne();
                DisposeEverything();

                Console.WriteLine(_calculationExceptionFromConsumerThread);
            }

            /// Аварийное завершение алгоритма из-за исключения, которое возникло в этом потоке
            void ForceQuitAndOutputException(Exception e)
            {
                allThreadsAreCompletedEvent.WaitOne();
                DisposeEverything();

                Console.WriteLine(e);
            }

            void DisposeEverything()
            {
                memoryBlockIsReleasedEvent.Dispose();
                memoryBlockIsReadyToGetEvent.Dispose();
                allThreadsAreCompletedEvent.Dispose();
            }
        }

        public void OnCalculationError(Exception e)
        {
            _calculationExceptionFromConsumerThread = e;
            _calculationErrorOccuredInConsumerThread = true;
        }
    }
}

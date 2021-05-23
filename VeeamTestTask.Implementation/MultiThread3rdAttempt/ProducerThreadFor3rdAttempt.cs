using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    public class ProducerThreadFor3rdAttempt : IChunkHashCalculator
    {
        public void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IChunkHashCalculator.ReturnResultDelegate callback)
        {
            using var fileStream = File.OpenRead(path);
            SplitFileAndCalculateHashes(fileStream, blockSize, hashAlgorithmName, callback);
        }

        public void SplitFileAndCalculateHashes(Stream fileStream, int blockSize, string hashAlgorithmName, IChunkHashCalculator.ReturnResultDelegate callback)
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
            // Если вдруг программа запущена не на Windows, ограничимся 8 блоками
            int amountOfBlocksAllowedInMemory = 8;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var ramCounter = new PerformanceCounter("Memory", "Available bytes");
                var availableMemory = Convert.ToUInt64(ramCounter?.NextValue());
                amountOfBlocksAllowedInMemory = (int)Math.Floor(availableMemory / (float)blockSize * 0.8);
            }

            // Если можно создать огромное количество блоков, нам они не понадобятся, 100 хватит в любом случае
            if (amountOfBlocksAllowedInMemory > 100)
            {
                amountOfBlocksAllowedInMemory = 100;
            }

            var allThreadsAreCompletedEvent = new AutoResetEvent(false);
            var fileHasEndedEvent = new FileHasEndedEvent();
            ThreadCounterFor3rdAttempt.Initialize(allThreadsAreCompletedEvent);

            var memoryBlockIsReleasedEvent = new ManualResetEventSlim(true);
            var releasedMemoryBlocks = new MemoryBlocksManagerFor3rdAttempt<byte[]>(amountOfBlocksAllowedInMemory, memoryBlockIsReleasedEvent);

            var memoryBlockIsReadyToGetEvent = new ManualResetEventSlim(true);
            var readyToGetMemoryBlocks = new MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock>(amountOfBlocksAllowedInMemory, memoryBlockIsReadyToGetEvent);
            fileHasEndedEvent.OnFileHasEnded += readyToGetMemoryBlocks.OnFileHasEnded;

            var chunkIndex = 1;
            var numberOfBytes = 1;

            for (; chunkIndex <= amountOfBlocksAllowedInMemory; chunkIndex++)
            {
                var currentBuffer = new byte[blockSize];
                numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);

                if (numberOfBytes == 0)
                {
                    fileHasEndedEvent.Fire();
                    memoryBlockIsReadyToGetEvent.Set();         // Заставляем треды посмотреть в очередь еще раз
                    WaitAndCleanUp();
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
                                    threadCallback: callback
                             ));

                fileHasEndedEvent.OnFileHasEnded += thread.OnFileHasEnded;
            }

            while (true)
            {
                Debug.WriteLine("Producer thread is trying to get released memory block");
                if (!releasedMemoryBlocks.TryDequeue(out var currentBuffer))
                {
                    Debug.WriteLine("Producer thread got nothing");
                    continue;
                }

                Debug.WriteLine("Producer thread got free memory block");

                numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);
                if (numberOfBytes == 0)
                {
                    fileHasEndedEvent.Fire();
                    memoryBlockIsReadyToGetEvent.Set();         // Заставляем треды посмотреть в очередь еще раз
                    WaitAndCleanUp();
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

            void WaitAndCleanUp()
            {
                allThreadsAreCompletedEvent.WaitOne();
                var _ = ThreadSafeResultWriter.HasMessagesInBuffer;

                memoryBlockIsReleasedEvent.Dispose();
                memoryBlockIsReadyToGetEvent.Dispose();
                allThreadsAreCompletedEvent.Dispose();
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using VeeamTestTask.Contracts;
using VeeamTestTask.Implementation.MultiThread;

namespace VeeamTestTask.Implementation.MultiThread2ndAttempt
{
    // Намного более быстрая реализация
    // Построена на идее zero-allocation. В первых шагах алгоритма мы размечаем несколько буферов, в которые записываются части файла.
    // Буфер отправляется в новый поток для расчета хэша, потом, после окончания расчета, в этот же буфер записывается следующая часть файла.
    // Это позволяет нам не создавать для кажого потока новый буфер и не тратиться на GC
    public class MultiThreadChunkHashCalculator2ndAttempt : IChunkHashCalculator
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

            MemoryBlocksManager.Initialize(amountOfBlocksAllowedInMemory);

            /// Каждый блок мы сохраняем в массив, чтобы GC их не убирал
            var memoryBlocks = new byte[amountOfBlocksAllowedInMemory][];
            for (int i = 0; i < memoryBlocks.Length; i++)
            {
                memoryBlocks[i] = new byte[blockSize];
                MemoryBlocksManager.ReleaseBlock(i);
            }

            var chunkIndex = 1;
            var numberOfBytes = 0;
            var parameterizedThreadStart = new ParameterizedThreadStart(ComputeHashDelegate);

            while (true)
            {
                // Дожидаемся освобождения любого блока памяти
                var firstAvailableMemoryBlock = MemoryBlocksManager.GetFirstFreeBlock();
                var currentBuffer = memoryBlocks[firstAvailableMemoryBlock];

                // Записываем в него часть файла
                numberOfBytes = fileStream.Read(currentBuffer, 0, blockSize);
                if (numberOfBytes == 0)
                {
                    ThreadCounter.WaitUntilAllWorkIsDone();
                    break;
                }

                if (blockSize > numberOfBytes)
                {
                    currentBuffer = currentBuffer[0..numberOfBytes];
                }

                ThreadCounter.Increment();
                MemoryBlocksManager.TakeBlock(firstAvailableMemoryBlock);

                // Отправляем ссылку на блок памяти в поток, предварительно поставив пометку, что блок занят
                new Thread(parameterizedThreadStart).Start(
                    new HashCalculationThreadParamsFor2ndAttempt(
                        memoryBlockIndex: firstAvailableMemoryBlock,
                        chunkIndex: chunkIndex,
                        bufferToHash: currentBuffer,
                        hashAlgorithmName: hashAlgorithmName,
                        threadCallback: callback
                    ));

                chunkIndex++;
            }
        }

        /// <summary>
        /// Действие, которое будет выполнено в потоке
        /// </summary>
        /// <param name="param">Объект класса HashCalculationThreadParams, который параметризует расчет хэша</param>
        public static void ComputeHashDelegate(object param)
        {
            var hashCalculationThreadParams = (HashCalculationThreadParamsFor2ndAttempt)param;

            try
            {
                // Объект алгоритма хэширования должен быть разный для каждого треда, иначе получим одинаковые хэши на выходе
                using var hashAlgorithm = HashAlgorithm.Create(hashCalculationThreadParams.HashAlgorithmName);

                var hashBytes = hashAlgorithm.ComputeHash(hashCalculationThreadParams.BufferToHash);

                MemoryBlocksManager.ReleaseBlock(hashCalculationThreadParams.MemoryBlockIndex);

                hashCalculationThreadParams.ThreadCallback(hashCalculationThreadParams.ChunkIndex, hashBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e}");
            }

            ThreadCounter.Decrement();
        }
    }
}

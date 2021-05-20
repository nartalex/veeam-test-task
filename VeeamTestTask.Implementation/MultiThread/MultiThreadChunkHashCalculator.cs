﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    public class MultiThreadChunkHashCalculator : IChunkHashCalculator
    {
        /// <inheritdoc/>
        public void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IChunkHashCalculator.ReturnResultDelegate callback)
        {
            using var fileStream = File.OpenRead(path);
            SplitFileAndCalculateHashes(fileStream, blockSize, hashAlgorithmName, callback);
        }

        /// <inheritdoc/>
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

            byte[] buffer = new byte[blockSize];
            var chunkIndex = 1;
            var numberOfBytes = 0;

            using var bufferedStream = new BufferedStream(fileStream);
            var parameterizedThreadStart = new ParameterizedThreadStart(ComputeHashDelegate);

            while ((numberOfBytes = bufferedStream.Read(buffer, 0, blockSize)) != 0)
            {
                Debug.Write($"Starting thread with chunk index {chunkIndex}. ");

                new Thread(parameterizedThreadStart).Start(new HashCalculationThreadParams(chunkIndex++, buffer[0..numberOfBytes], hashAlgorithmName, callback));
                ThreadCounter.Increment();
                ThreadCounter.WaitUntilThreadsAreAvailable();

                Debug.WriteLine($"Bytes left: {fileStream.Length - fileStream.Position}");

                // Здесь оптимизация выше уже не работает
                // Почему-то на последних блоках fileStream.Position приравнивается к fileStream.Length, 
                // что делает bytesLeft равным нулю
                //bytesLeft = fileStream.Length - fileStream.Position;
                //if (bytesLeft < blockSize)
                //{
                //    blockSize = (int)bytesLeft;
                //}

                // Делая здесь новый массив, мы заменяем ссылку buffer на новую, и следующий блок
                // будет писаться уже в новый массив, тогда как старая ссылка записана в HashCalculationThreadParams.
                // Это позволяет нам не копировать массив, уменьшая трафик памяти
                buffer = new byte[blockSize];
            }

            // Так как есть команда только на старт потоков, мы не можем отследить их завершение без костылей
            // Этот метод ждет, пока выполнятся все потоки и очистится буфер вывода
            // Иначе процесс завершится без завершения потоков
            ThreadCounter.WaitUntilAllWorkIsDone();
        }

        /// <summary>
        /// Действие, которое будет выполнено в потоке
        /// </summary>
        /// <param name="param">Объект класса HashCalculationThreadParams, который параметризует расчет хэша</param>
        public static void ComputeHashDelegate(object param)
        {
            var hashCalculationThreadParams = (HashCalculationThreadParams)param;

            // Объект алгоритма хэширования должен быть разный для каждого треда, иначе получим одинаковые хэши на выходе
            using var hashAlgorithm = HashAlgorithm.Create(hashCalculationThreadParams.HashAlgorithmName);

            var hashBytes = hashAlgorithm.ComputeHash(hashCalculationThreadParams.BufferToHash);

            hashCalculationThreadParams.ThreadCallback(hashCalculationThreadParams.ChunkIndex, hashBytes);

            ThreadCounter.Decrement();
        }
    }
}

using System;
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
            byte[] buffer = new byte[blockSize];
            var chunkIndex = 1;

            using var bufferedStream = new BufferedStream(fileStream);
            var parameterizedThreadStart = new ParameterizedThreadStart(ComputeHashDelegate);

            while (bufferedStream.Read(buffer, 0, blockSize) != 0)
            {
                // Мы не можем передавать в поток buffer, потому что сразу же после этого
                // цикл начнется заново и перезапишет массив, а поток будет работать с новыми байтами.
                // Поэтому создадим копию массива и передадим в поток именно копию
                var arrayCopy = new byte[blockSize];
                Array.Copy(buffer, arrayCopy, blockSize);

                new Thread(parameterizedThreadStart).Start(new HashCalculationThreadParams(chunkIndex++, arrayCopy, hashAlgorithmName, callback));
                ThreadCounter.Increment();
                ThreadCounter.WaitUntilThreadsAreAvailable();
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

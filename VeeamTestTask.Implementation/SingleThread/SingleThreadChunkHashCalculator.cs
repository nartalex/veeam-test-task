using System;
using System.IO;
using System.Security.Cryptography;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.SingleThread
{
    public class SingleThreadChunkHashCalculator : IChunkHashCalculator
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
            var numberOfBytes = 0;

            using var bufferedStream = new BufferedStream(fileStream);
            using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName);

            while ((numberOfBytes = bufferedStream.Read(buffer, 0, blockSize)) != 0)
            {
                // Если мы находимся в последнем блоке, часть массива будет занята нулями
                // Чтобы не расчитывать хэш для нулевой части, укажем явно границы массива
                var hashBytes = hashAlgorithm.ComputeHash(buffer[0..numberOfBytes]);
                callback(chunkIndex++, hashBytes);
            }
        }
    }
}


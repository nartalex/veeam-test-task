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

            using var bufferedStream = new BufferedStream(fileStream);
            using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName);

            while (bufferedStream.Read(buffer, 0, blockSize) != 0)
            {
                var hashBytes = hashAlgorithm.ComputeHash(buffer);
                callback(chunkIndex++, hashBytes);
            }
        }
    }
}


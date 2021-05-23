using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.SingleThread
{
    public class SingleThreadChunkHashCalculator : IChunkHashCalculator
    {
        /// <inheritdoc/>
        public void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            using var fileStream = File.OpenRead(path);
            SplitFileAndCalculateHashes(fileStream, blockSize, hashAlgorithmName, resultWriter);
        }

        /// <inheritdoc/>
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

            byte[] buffer = new byte[blockSize];
            var chunkIndex = 1;
            var numberOfBytes = 0;
            using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName);

            while ((numberOfBytes = fileStream.Read(buffer, 0, blockSize)) != 0)
            {
                // Если мы находимся в последнем блоке, часть массива будет занята нулями
                // Чтобы не расчитывать хэш для нулевой части, укажем явно границы массива
                if (blockSize > numberOfBytes)
                {
                    buffer = buffer[0..numberOfBytes];
                }

                var hashBytes = hashAlgorithm.ComputeHash(buffer);
                resultWriter.Write(chunkIndex++, hashBytes);
            }
        }
    }
}


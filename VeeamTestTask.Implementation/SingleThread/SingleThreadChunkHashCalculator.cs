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
            var bytesLeft = fileStream.Length;
            byte[] buffer = new byte[blockSize];
            var chunkIndex = 1;
            var numberOfBytes = 0;
            using var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName);

            while (true)
            {
                // Это позволяет нам не создавать слишком большой массив буффера, если файл сам по себе меньше размера блока
                // А так же, если мы находимся в последнем блоке, часть массива будет занята нулями,
                // чтобы не расчитывать хэш для нулевой части, укажем явно границы массива
                if (bytesLeft < blockSize)
                {
                    blockSize = (int)bytesLeft;
                }

                numberOfBytes = fileStream.Read(buffer, 0, blockSize);
                if(numberOfBytes == 0)
                {
                    break;
                }

                var hashBytes = hashAlgorithm.ComputeHash(buffer, 0, blockSize);
                resultWriter.Write(chunkIndex, hashBytes);

                bytesLeft = fileStream.Length - fileStream.Position;
                chunkIndex++;
            }
        }
    }
}


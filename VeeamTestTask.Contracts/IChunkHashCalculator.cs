using System.IO;

namespace VeeamTestTask.Contracts
{
    public interface IChunkHashCalculator
    {
        /// <summary>
        /// Делегат для вывода результата
        /// Вызывается после расчета каждого блока
        /// </summary>
        /// <param name="chunkIndex">Индекс блока файла</param>
        /// <param name="hashBytes">Хэш блока</param>
        public delegate void ReturnResultDelegate(int chunkIndex, byte[] hashBytes);

        /// <summary>
        /// Разделить файл и посчитать хэши блоков
        /// </summary>
        /// <param name="path">Путь до файла</param>
        /// <param name="blockSize">Размер блока в байтах</param>
        /// <param name="hashAlgorithmName">Название алгоритма хэширования</param>
        /// <param name="callback">Действие при завершении расчета хэша каждого блока</param>
        void SplitFileAndCalculateHashes(string path, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter);

        /// <summary>
        /// Разделить файл и посчитать хэши блоков
        /// </summary>
        /// <param name="fileStream">Стрим, содержащий файл</param>
        /// <param name="blockSize">Размер блока в байтах</param>
        /// <param name="hashAlgorithmName">Название алгоритма хэширования</param>
        /// <param name="callback">Действие при завершении расчета хэша каждого блока</param>
        void SplitFileAndCalculateHashes(Stream fileStream, int blockSize, string hashAlgorithmName, IBufferedResultWriter resultWriter);
    }
}

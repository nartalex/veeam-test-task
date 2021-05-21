using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread2ndAttempt
{
    /// <summary>
    /// Менеджер блоков памяти для второй реализации алгоритма
    /// </summary>
    internal static class MemoryBlocksManager
    {
        private static Dictionary<int, bool> _memoryBlocksAvailabiliryStorage;
        private static object _lockObject = new();

        public static void Initialize(int capacity)
        {
            if (_memoryBlocksAvailabiliryStorage == null)
            {
                lock (_lockObject)
                {
                    if (_memoryBlocksAvailabiliryStorage == null)
                    {
                        _memoryBlocksAvailabiliryStorage = new(capacity);
                    }
                }
            }
        }

        /// <summary>
        /// Занять блок памяти
        /// </summary>
        /// <param name="blockIndex">Индекс блока</param>
        public static void TakeBlock(int blockIndex)
        {
            lock (_lockObject)
            {
                _memoryBlocksAvailabiliryStorage[blockIndex] = false;
            }
        }

        /// <summary>
        /// Освободить блок памяти
        /// </summary>
        /// <param name="blockIndex">Индекс блока</param>
        public static void ReleaseBlock(int blockIndex)
        {
            lock (_lockObject)
            {
                _memoryBlocksAvailabiliryStorage[blockIndex] = true;
            }

            Debug.WriteLine($"Block released: {blockIndex}");
        }

        /// <summary>
        /// Получить первый свободный блок памяти. Если свободных нет - ждать освобождения
        /// </summary>
        /// <returns>Индекс блока</returns>
        public static int GetFirstFreeBlock()
        {
            while (true)
            {
                lock (_lockObject)
                {
                    for (int i = 0; i < _memoryBlocksAvailabiliryStorage.Count; i++)
                    {
                        if (_memoryBlocksAvailabiliryStorage[i] == true)
                        {
                            return i;
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}

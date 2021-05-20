using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    /// <summary>
    /// Счетчик ресурсов
    /// </summary>
    public class ResourceWatcher
    {
        private static int _threadCounter = 0;
        private static PerformanceCounter _ramCounter;
        private const float _blockSizeMultiplier = 1.5f;

        static ResourceWatcher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _ramCounter = new PerformanceCounter("Memory", "Available bytes");
            }
        }

        private ResourceWatcher()
        {
        }

        /// <summary>
        /// Ограничение количества потоков на алгоритм расчета хэша
        /// </summary>
        public static int MaxThreadNumber => Environment.ProcessorCount;

        /// <summary>
        /// Увеличить счетчик активных потоков
        /// Применяется сразу после создания потока
        /// </summary>
        public static void IncrementThreadCounter()
        {
            Interlocked.Increment(ref _threadCounter);
        }

        /// <summary>
        /// Уменьшить счетчик активных потоков
        /// Применяется сразу после завершения потока
        /// </summary>
        public static void DecrementThreadCounter()
        {
            Interlocked.Decrement(ref _threadCounter);
        }

        /// <summary>
        /// Ожидание освобождения потоков, если их количество превысило количество логических ядер
        /// </summary>
        public static void WaitUntilResourcesAreAvailable(int blockSize)
        {
            while (_threadCounter >= MaxThreadNumber || !IsMemoryAvailableEnought(blockSize))
            {
                Thread.Sleep(100);
            }
        }

        public static bool IsMemoryAvailableEnought(int blockSize)
        {
            return (_ramCounter?.NextValue() ?? int.MaxValue) >= blockSize * _blockSizeMultiplier;
        }

        /// <summary>
        /// Ожидание завершения всех потоков и очистки буфера
        /// </summary>
        public static void WaitUntilAllWorkIsDone()
        {
            while (_threadCounter != 0 || ThreadSafeResultWriter.HasMessagesInBuffer)
            {
                Thread.Sleep(100);
            }
        }
    }
}

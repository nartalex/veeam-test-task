using System;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    /// <summary>
    /// Счетчик активных потоков
    /// </summary>
    internal class ThreadCounter
    {
        private static int _counter = 0;

        private ThreadCounter()
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
        public static void Increment()
        {
            Interlocked.Increment(ref _counter);
        }

        /// <summary>
        /// Уменьшить счетчик активных потоков
        /// Применяется сразу после завершения потока
        /// </summary>
        public static void Decrement()
        {
            Interlocked.Decrement(ref _counter);
        }

        /// <summary>
        /// Ожидание освобождения потоков, если их количество превысило количество логических ядер
        /// </summary>
        public static void WaitUntilThreadsAreAvailable()
        {
            while(_counter >= MaxThreadNumber)
            {
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Ожидание завершения всех потоков и очистки буфера
        /// </summary>
        public static void WaitUntilAllWorkIsDone()
        {
            while (_counter != 0 || ThreadSafeResultWriter.HasMessagesInBuffer)
            {
                Thread.Sleep(100);
            }
        }
    }
}

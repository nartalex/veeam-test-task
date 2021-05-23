using System.Diagnostics;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal static class ThreadCounter
    {
        private static int _activeThreadsNumber;
        private static AutoResetEvent _allThreadsAreCompletedEvent;
        private static readonly object _lockObject = new();

        public static void Initialize(AutoResetEvent allThreadsAreCompletedEvent)
        {
            _allThreadsAreCompletedEvent = allThreadsAreCompletedEvent;
        }

        /// <summary>
        /// Увеличить счетчик активных потоков
        /// Применяется сразу после создания потока
        /// </summary>
        public static void Increment()
        {
            Interlocked.Increment(ref _activeThreadsNumber);
        }

        /// <summary>
        /// Уменьшить счетчик активных потоков
        /// Применяется сразу после завершения потока
        /// </summary>
        public static void Decrement()
        {
            lock (_lockObject)
            {
                Interlocked.Decrement(ref _activeThreadsNumber);

                if (_activeThreadsNumber == 0)
                {
                    Debug.WriteLine("All threads are completed");
                    _allThreadsAreCompletedEvent?.Set();
                }
            }
        }
    }
}

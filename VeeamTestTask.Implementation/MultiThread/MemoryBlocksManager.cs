using System.Collections.Generic;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread
{
    /// <summary>
    /// Менеджер памяти, который синхронизирует работу потоков и занимается обработкой блоков памяти
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MemoryBlocksManager<T>
    {
        private readonly Queue<T> _memoryBlocks;
        private readonly ManualResetEventSlim _elementAvailabilityEvent;
        private readonly ManualResetEventSlim _fileHasEndedEvent;
        private readonly object _lockObject = new();
        private bool _isAborted = false;

        public MemoryBlocksManager(int capacity, ManualResetEventSlim elementAvailabilityEvent, ManualResetEventSlim fileHasEndedEvent)
        {
            _memoryBlocks = new Queue<T>(capacity);
            _elementAvailabilityEvent = elementAvailabilityEvent;
            _fileHasEndedEvent = fileHasEndedEvent;
        }

        /// <summary>
        /// Записать элемент в очередь. Поднимает событие доступности элемента
        /// </summary>
        /// <param name="memoryBlock"></param>
        public void Enqueue(T memoryBlock)
        {
            if (_isAborted)
            {
                return;
            }

            lock (_lockObject)
            {
                _memoryBlocks.Enqueue(memoryBlock);
            }

            _elementAvailabilityEvent.Set();
        }

        /// <summary>
        /// Текущее количество элементов в очереди
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            lock (_lockObject)
            {
                return _memoryBlocks.Count;
            }
        }

        /// <summary>
        /// Дождаться события доступности элемента и достать элемент из очереди.
        /// Если после текущего элемента в очереди есть еще элементы, событие доступности не снимается
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryDequeue(out T result)
        {
            _elementAvailabilityEvent.Wait();

            if (_isAborted)
            {
                result = default;
                return false;
            }

            lock (_lockObject)
            {
                var isElementAvailable = _memoryBlocks.TryDequeue(out result);

                // Сбрасываем доступность элементов если очередь уже пуста при условии, что файл еще читается
                if (_memoryBlocks.Count == 0 && !_fileHasEndedEvent.IsSet)
                {
                    _elementAvailabilityEvent.Reset();
                }

                return isElementAvailable;
            }
        }

        public void AbortExecution()
        {
            _isAborted = true;
            _memoryBlocks.Clear();
            _elementAvailabilityEvent.Set();
        }
    }
}

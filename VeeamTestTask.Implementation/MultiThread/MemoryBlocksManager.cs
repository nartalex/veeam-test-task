using System.Collections.Generic;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread
{
    /// <summary>
    /// Менеджер памяти, который синхронизирует работу потоков и занимается обработкой блоков памяти
    /// </summary>
    /// <typeparam name="T">Тип данных, который будет представлен в блоках памяти</typeparam>
    public class MemoryBlocksManager<T>
    {
        private readonly Queue<T> _memoryBlocks;
        private readonly ManualResetEventSlim _elementAvailabilityEvent;
        private readonly ManualResetEventSlim _endOfFileEvent;
        private readonly ManualResetEventSlim _abortExecutionEvent;
        private readonly object _lockObject = new();

        /// <summary>
        /// Инициализирует менеджер
        /// </summary>
        /// <param name="capacity">Начальная длина коллекции</param>
        /// <param name="elementAvailabilityEvent">Событие для сообщения о доступности элемента</param>
        /// <param name="endOfFileEvent">Событие для сообщения об окончании файла</param>
        /// <param name="abortExecutionEvent">Событие для сообщения об ошибке</param>
        public MemoryBlocksManager(int capacity, ManualResetEventSlim elementAvailabilityEvent, ManualResetEventSlim endOfFileEvent, ManualResetEventSlim abortExecutionEvent)
        {
            _memoryBlocks = new Queue<T>(capacity);
            _elementAvailabilityEvent = elementAvailabilityEvent;
            _endOfFileEvent = endOfFileEvent;
            _abortExecutionEvent = abortExecutionEvent;
        }

        /// <summary>
        /// Записать элемент в очередь. Поднимает событие доступности элемента
        /// </summary>
        /// <param name="memoryBlock"></param>
        public void Enqueue(T memoryBlock)
        {
            if (_abortExecutionEvent.IsSet)
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

            if (_abortExecutionEvent.IsSet)
            {
                result = default;
                return false;
            }

            lock (_lockObject)
            {
                var isElementAvailable = _memoryBlocks.TryDequeue(out result);

                // Сбрасываем доступность элементов если очередь уже пуста при условии, что файл еще читается
                if (_memoryBlocks.Count == 0 && !_endOfFileEvent.IsSet)
                {
                    _elementAvailabilityEvent.Reset();
                }

                return isElementAvailable;
            }
        }
    }
}

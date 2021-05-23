using System.Collections.Generic;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    public class MemoryBlocksManagerFor3rdAttempt<T>
    {
        private readonly Queue<T> _memoryBlocks;
        private readonly ManualResetEventSlim _elementAvailabilityEvent;
        private readonly object _lockObject = new();
        private bool _fileHasEnded = false;

        public MemoryBlocksManagerFor3rdAttempt(int capacity, ManualResetEventSlim elementAvailabilityEvent)
        {
            _memoryBlocks = new Queue<T>(capacity);
            _elementAvailabilityEvent = elementAvailabilityEvent;
        }

        public void Enqueue(T memoryBlock)
        {
            lock (_lockObject)
            {
                _memoryBlocks.Enqueue(memoryBlock);
            }

            _elementAvailabilityEvent.Set();
        }

        public int Count()
        {
            lock (_lockObject)
            {
                return _memoryBlocks.Count;
            }
        }

        public bool TryDequeue(out T result)
        {
            _elementAvailabilityEvent.Wait();

            lock (_lockObject)
            {
                var isElementAvailable = _memoryBlocks.TryDequeue(out result);

                if (_memoryBlocks.Count == 0 && !_fileHasEnded)
                {
                    _elementAvailabilityEvent.Reset();
                }

                return isElementAvailable;
            }
        }

        public void OnFileHasEnded()
        {
            _fileHasEnded = true;
        }
    }
}

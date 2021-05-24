using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal class ConsumerThread
    {
        private readonly Thread _currentThread;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly ConsumerThreadParams _params;

        public ConsumerThread(ConsumerThreadParams param)
        {
            _params = param;
            _hashAlgorithm = HashAlgorithm.Create(param.HashAlgorithmName);
            _currentThread = new Thread(new ThreadStart(DoWork));
            _currentThread.Name = param.ThreadName;

            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is created");
            ThreadCounter.Increment();
            _currentThread.Start();
        }

        public void DoWork()
        {
            try
            {
                while (!_params.ExitByErrorEvent.IsSet)
                {
                    Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is trying to get memory block");

                    // Получаем блок файла для хэширования
                    if (!_params.ReadyToGetMemoryBlocks.TryDequeue(out var currentChunk))
                    {
                        // Если мы не получили блок и получили сообщение об окончании файла - гасим поток
                        if (_params.ExitByFileEndingEvent.IsSet)
                        {
                            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is shutting down");
                            break;
                        }
                        // Если мы не получили блок и сообщения об окончании файла еще не было, то другие потоки опередили текущий
                        else
                        {
                            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} got nothing");
                            continue;
                        }
                    }

                    Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} got chunk #{currentChunk.ChunkIndex}");

                    var hashBytes = _hashAlgorithm.ComputeHash(currentChunk.MemoryBlock);

                    // Блок файла прохеширован, область памяти можно освобождать под следующий блок
                    _params.ReleasedMemoryBlocks.Enqueue(currentChunk.MemoryBlock);

                    _params.ResultWriter.Write(currentChunk.ChunkIndex, hashBytes);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} caused exception");
                Debug.WriteLine(e);
                _params.NotifyProducerAboutError(e);
                _params.ExitByErrorEvent.Set();
            }

            ThreadCounter.Decrement();
            _hashAlgorithm.Dispose();
        }
    }
}

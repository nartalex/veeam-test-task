using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    internal class ConsumerThreadFor3rdAttempt
    {
        private readonly Thread _currentThread;
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly HashCalculationThreadParamsFor3rdAttempt _params;
        private bool _fileHasEnded = false;

        public ConsumerThreadFor3rdAttempt(HashCalculationThreadParamsFor3rdAttempt param)
        {
            _params = param;
            _hashAlgorithm = HashAlgorithm.Create(param.HashAlgorithmName);
            _currentThread = new Thread(new ThreadStart(DoWork));

            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is created");
            ThreadCounterFor3rdAttempt.Increment();
            _currentThread.Start();
        }

        public void DoWork()
        {
            while (true)
            {
                Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is trying to get memory block");

                //ReadyToGetMemoryBlock currentChunk;

                //if (_fileHasEnded && !_params.ReadyToGetMemoryBlocks.TryDequeue(out currentChunk))
                //{
                //    Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is shutting down");
                //    break;
                //}
                //else
                //{
                //    _params.MemoryBlockIsReadyToGetEvent.WaitOne();
                //    if (!_params.ReadyToGetMemoryBlocks.TryDequeueAndWatchForNextBlock(out currentChunk, _params.MemoryBlockIsReadyToGetEvent))
                //    {
                //        Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} got nothing");
                //        continue;
                //    }
                //}

                if (!_params.ReadyToGetMemoryBlocks.TryDequeue(out var currentChunk))
                {
                    if (_fileHasEnded)
                    {
                        Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is shutting down");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} got nothing");
                        continue;
                    }
                }

                Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} got chunk #{currentChunk.ChunkIndex}");

                var hashBytes = _hashAlgorithm.ComputeHash(currentChunk.MemoryBlock);

                _params.ReleasedMemoryBlocks.Enqueue(currentChunk.MemoryBlock);

                _params.ThreadCallback(currentChunk.ChunkIndex, hashBytes);
            }

            ThreadCounterFor3rdAttempt.Decrement();
            _hashAlgorithm.Dispose();
        }

        public void OnFileHasEnded()
        {
            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} has recieved message about file ending");

            _fileHasEnded = true;
        }
    }
}

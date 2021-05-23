﻿using System;
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
        private bool _calculationErrorOccured = false;

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
            while (!_calculationErrorOccured)
            {
                try
                {
                    Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} is trying to get memory block");

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

                    _params.ResultWriter.Write(currentChunk.ChunkIndex, hashBytes);
                }
                catch(Exception e)
                {
                    _calculationErrorOccured = true;
                    _params.CalculationErrorEvent.Fire(e);
                }
            }

            ThreadCounterFor3rdAttempt.Decrement();
            _hashAlgorithm.Dispose();
        }

        public void OnFileHasEnded()
        {
            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} has recieved message about file ending");

            _fileHasEnded = true;
        }

        public void OnCalculationError(Exception e)
        {
            Debug.WriteLine($"Consumer thread {_currentThread.ManagedThreadId} has recieved message about calculation error");

            _calculationErrorOccured = true;
        }
    }
}

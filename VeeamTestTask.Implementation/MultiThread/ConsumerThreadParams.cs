﻿using System;
using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal class ConsumerThreadParams
    {
        public ConsumerThreadParams(
            string threadName,
            MemoryBlocksManager<byte[]> releasedMemoryBlocks,
            MemoryBlocksManager<ReadyToGetMemoryBlock> readyToGetMemoryBlocks,
            string hashAlgorithmName,
            IBufferedResultWriter resultWriter,
            ManualResetEventSlim exitByFileEndingEvent,
            ManualResetEventSlim exitByErrorEvent,
            Action<Exception> errorNotifierMethod)
        {
            ThreadName = threadName;
            ReleasedMemoryBlocks = releasedMemoryBlocks;
            ReadyToGetMemoryBlocks = readyToGetMemoryBlocks;
            HashAlgorithmName = hashAlgorithmName;
            ResultWriter = resultWriter;
            ExitByFileEndingEvent = exitByFileEndingEvent;
            ExitByErrorEvent = exitByErrorEvent;
            NotifyProducerAboutError = errorNotifierMethod;
        }

        public string ThreadName { get; set; }

        public MemoryBlocksManager<byte[]> ReleasedMemoryBlocks { get; }

        public MemoryBlocksManager<ReadyToGetMemoryBlock> ReadyToGetMemoryBlocks { get; }

        public string HashAlgorithmName { get; }

        public IBufferedResultWriter ResultWriter { get; }

        public ManualResetEventSlim ExitByFileEndingEvent { get; set; }

        public ManualResetEventSlim ExitByErrorEvent { get; set; }

        public Action<Exception> NotifyProducerAboutError { get; set; }
    }
}

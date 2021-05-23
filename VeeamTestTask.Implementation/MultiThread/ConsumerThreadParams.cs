using VeeamTestTask.Contracts;
using VeeamTestTask.Implementation.MultiThread.Events;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal class ConsumerThreadParams
    {
        public ConsumerThreadParams(
            MemoryBlocksManager<byte[]> releasedMemoryBlocks,
            MemoryBlocksManager<ReadyToGetMemoryBlock> readyToGetMemoryBlocks,
            string hashAlgorithmName,
            IBufferedResultWriter resultWriter,
            CalculationErrorEvent calculationErrorEvent)
        {
            ReleasedMemoryBlocks = releasedMemoryBlocks;
            ReadyToGetMemoryBlocks = readyToGetMemoryBlocks;
            HashAlgorithmName = hashAlgorithmName;
            ResultWriter = resultWriter;
            CalculationErrorEvent = calculationErrorEvent;
        }

        public MemoryBlocksManager<byte[]> ReleasedMemoryBlocks { get; }

        public MemoryBlocksManager<ReadyToGetMemoryBlock> ReadyToGetMemoryBlocks { get; }

        public string HashAlgorithmName { get; }

        public IBufferedResultWriter ResultWriter { get; }

        public CalculationErrorEvent CalculationErrorEvent { get; set; }
    }
}

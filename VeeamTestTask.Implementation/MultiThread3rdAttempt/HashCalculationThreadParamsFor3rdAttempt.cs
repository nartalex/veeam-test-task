using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    internal class HashCalculationThreadParamsFor3rdAttempt
    {
        public HashCalculationThreadParamsFor3rdAttempt(
            MemoryBlocksManagerFor3rdAttempt<byte[]> releasedMemoryBlocks,
            MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock> readyToGetMemoryBlocks,
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

        public MemoryBlocksManagerFor3rdAttempt<byte[]> ReleasedMemoryBlocks { get; }

        public MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock> ReadyToGetMemoryBlocks { get; }

        public string HashAlgorithmName { get; }

        public IBufferedResultWriter ResultWriter { get; }

        public CalculationErrorEvent CalculationErrorEvent { get; set; }
    }
}

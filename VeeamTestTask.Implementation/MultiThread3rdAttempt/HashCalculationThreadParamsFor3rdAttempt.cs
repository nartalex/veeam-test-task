using System.Threading;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
{
    internal class HashCalculationThreadParamsFor3rdAttempt
    {
        public HashCalculationThreadParamsFor3rdAttempt(
            MemoryBlocksManagerFor3rdAttempt<byte[]> releasedMemoryBlocks,
            MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock> readyToGetMemoryBlocks,
            string hashAlgorithmName,
            IChunkHashCalculator.ReturnResultDelegate threadCallback)
        {
            ReleasedMemoryBlocks = releasedMemoryBlocks;
            ReadyToGetMemoryBlocks = readyToGetMemoryBlocks;
            HashAlgorithmName = hashAlgorithmName;
            ThreadCallback = threadCallback;
        }

        public MemoryBlocksManagerFor3rdAttempt<byte[]> ReleasedMemoryBlocks { get; }

        public MemoryBlocksManagerFor3rdAttempt<ReadyToGetMemoryBlock> ReadyToGetMemoryBlocks { get; }

        public string HashAlgorithmName { get; }

        public IChunkHashCalculator.ReturnResultDelegate ThreadCallback { get; }
    }
}

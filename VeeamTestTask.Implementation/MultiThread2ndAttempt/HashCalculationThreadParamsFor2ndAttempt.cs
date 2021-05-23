using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread2ndAttempt
{
    internal class HashCalculationThreadParamsFor2ndAttempt
    {
        public HashCalculationThreadParamsFor2ndAttempt(int memoryBlockIndex, int chunkIndex, byte[] bufferToHash, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            MemoryBlockIndex = memoryBlockIndex;
            ChunkIndex = chunkIndex;
            BufferToHash = bufferToHash;
            HashAlgorithmName = hashAlgorithmName;
            ResultWriter = resultWriter;
        }

        public int MemoryBlockIndex { get; set; }

        public int ChunkIndex { get; set; }

        public byte[] BufferToHash { get; set; }

        public string HashAlgorithmName { get; set; }

        public IBufferedResultWriter ResultWriter { get; set; }
    }
}

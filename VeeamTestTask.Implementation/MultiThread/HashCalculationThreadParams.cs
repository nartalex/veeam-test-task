using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal class HashCalculationThreadParams
    {
        public HashCalculationThreadParams(int chunkIndex, byte[] bufferToHash, string hashAlgorithmName, IChunkHashCalculator.ReturnResultDelegate threadCallback)
        {
            ChunkIndex = chunkIndex;
            BufferToHash = bufferToHash;
            HashAlgorithmName = hashAlgorithmName;
            ThreadCallback = threadCallback;
        }

        public int ChunkIndex { get; set; }

        public byte[] BufferToHash { get; set; }

        public string HashAlgorithmName { get; set; }

        public IChunkHashCalculator.ReturnResultDelegate ThreadCallback { get; set; }
    }
}

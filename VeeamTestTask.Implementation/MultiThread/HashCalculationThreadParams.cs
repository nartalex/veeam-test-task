using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation.MultiThread
{
    internal class HashCalculationThreadParams
    {
        public HashCalculationThreadParams(int chunkIndex, byte[] bufferToHash, string hashAlgorithmName, IBufferedResultWriter resultWriter)
        {
            ChunkIndex = chunkIndex;
            BufferToHash = bufferToHash;
            HashAlgorithmName = hashAlgorithmName;
            ResultWriter = resultWriter;
        }

        public int ChunkIndex { get; set; }

        public byte[] BufferToHash { get; set; }

        public string HashAlgorithmName { get; set; }

        public IBufferedResultWriter ResultWriter { get; set; }
    }
}

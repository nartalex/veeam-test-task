namespace VeeamTestTask.Implementation.MultiThread
{
    internal class ReadyToGetMemoryBlock
    {
        public ReadyToGetMemoryBlock(int chunkIndex, byte[] memoryBlock)
        {
            ChunkIndex = chunkIndex;
            MemoryBlock = memoryBlock;
        }

        public int ChunkIndex { get; }

        public byte[] MemoryBlock { get; }
    }
}

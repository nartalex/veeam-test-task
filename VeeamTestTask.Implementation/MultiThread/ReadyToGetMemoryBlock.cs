namespace VeeamTestTask.Implementation.MultiThread
{
    internal class ReadyToGetMemoryBlock
    {
        public ReadyToGetMemoryBlock(int chunkIndex, byte[] memoryBlock)
        {
            ChunkIndex = chunkIndex;
            MemoryBlock = memoryBlock;
        }

        public int ChunkIndex { get; set; }

        public byte[] MemoryBlock { get; set; }
    }
}

namespace VeeamTestTask.Contracts
{
    public interface IBufferedResultWriter
    {
        bool HasMessagesInBuffer { get; }

        void AbortOutput();

        void Write(int chunkIndex, byte[] hashBytes);
    }
}

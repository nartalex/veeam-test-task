using VeeamTestTask.Contracts;

namespace VeeamTestTask.Benchmark
{
    public class VoidResultWriter : IBufferedResultWriter
    {
        public bool HasMessagesInBuffer => false;

        public void AbortOutput()
        {
        }

        public void Write(int chunkIndex, byte[] hashBytes)
        {
        }
    }
}

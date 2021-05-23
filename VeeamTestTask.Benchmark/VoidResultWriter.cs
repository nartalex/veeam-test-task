using VeeamTestTask.Contracts;

namespace VeeamTestTask.Benchmark
{
    public class VoidResultWriter : ThreadSafeResultWriter
    {
        protected override void WriteHashToOutput(int chunkIndex, byte[] hashBytes)
        {
        }
    }
}

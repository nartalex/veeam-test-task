using System.IO;
using System.Text;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation
{
    public class FileResultWriter : ThreadSafeResultWriter
    {
        private readonly object _fileLock = new();
        private readonly Stream _outputStream;

        public FileResultWriter(string filepath)
        {
            _outputStream = File.Open(filepath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        protected override void WriteHashToOutput(int chunkIndex, byte[] hashBytes)
        {
            lock (_fileLock)
            {
                WriteToStream(chunkIndex.ToString());
                WriteToStream(". ");

                foreach (var @byte in hashBytes)
                {
                    WriteToStream(@byte.ToString("x2"));
                }

                WriteToStream("\n");
            }
        }

        private void WriteToStream(string text)
        {
            _outputStream.Write(Encoding.UTF8.GetBytes(text));
        }

        public override void Dispose()
        {
            _outputStream.Dispose();
            base.Dispose();
        }
    }
}

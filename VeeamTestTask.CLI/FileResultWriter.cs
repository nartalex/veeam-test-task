using System.IO;
using System.Text;
using VeeamTestTask.Implementation.MultiThread;

namespace VeeamTestTask.CLI
{
    public class FileResultWriter : ResultWriter
    {
        private static readonly object fileLock = new object();
        private static Stream outputStream;

        public FileResultWriter(string filepath)
        {
            outputStream = File.Open(filepath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        protected override void WriteHashToOutput(int chunkIndex, byte[] hashBytes)
        {
            lock (fileLock)
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

        private static void WriteToStream(string text)
        {
            outputStream.Write(Encoding.UTF8.GetBytes(text));
        }

        public override void Dispose()
        {
            outputStream.Dispose();
        }
    }
}

using System;
using System.IO;
using System.Text;
using VeeamTestTask.Implementation.MultiThread;

namespace VeeamTestTask.CLI
{
    public class FileResultWriter : ResultWriter, IDisposable
    {
        private static readonly object fileLock = new object();
        private static Stream outputStream;

        public FileResultWriter(string filepath)
        {
            outputStream = File.Open($"{filepath}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.log", FileMode.Create, FileAccess.Write, FileShare.Read);
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

        public void Dispose()
        {
            outputStream.Dispose();
        }
    }
}

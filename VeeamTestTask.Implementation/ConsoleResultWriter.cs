using System;
using VeeamTestTask.Contracts;

namespace VeeamTestTask.Implementation
{
    public class ConsoleResultWriter : ThreadSafeResultWriter
    {
        private readonly object _consoleLock = new();

        protected override void WriteHashToOutput(int chunkIndex, byte[] hashBytes)
        {
            lock (_consoleLock)
            {
                Console.Write(chunkIndex);
                Console.Write(". ");

                foreach (var @byte in hashBytes)
                {
                    Console.Write(@byte.ToString("x2"));
                }

                Console.WriteLine();
            }
        }

        public void WriteLineThreadSafe(string text)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(text);
            }
        }
    }
}

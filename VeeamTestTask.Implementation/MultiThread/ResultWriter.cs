using System;
using System.Collections.Generic;
using System.Linq;

namespace VeeamTestTask.Implementation.MultiThread
{
    public abstract class ResultWriter : IDisposable
    {
        /// <summary>
        /// Буфер сообщений, сделан для вывода хэшей в строгой последовательности
        /// </summary>
        private static Dictionary<int, byte[]> outputBuffer = null;
        private static readonly object creationLock = new object();
        private static readonly object bufferLock = new object();
        private static int lastChunkIndex = 0;

        protected ResultWriter()
        {
        }

        /// <summary>
        /// Thread-safe singleton
        /// </summary>
        private static Dictionary<int, byte[]> Buffer
        {
            get
            {
                if (outputBuffer == null)
                {
                    lock (creationLock)
                    {
                        if (outputBuffer == null)
                        {
                            outputBuffer = new(ThreadCounter.MaxThreadNumber);
                        }
                    }
                }

                return outputBuffer;
            }
        }

        /// <summary>
        /// Флаг, показывающий наличие сообщений в буфере
        /// </summary>
        public static bool HasMessagesInBuffer
        {
            get
            {
                lock (bufferLock)
                {
                    return outputBuffer.Any();
                }
            }
        }

        /// <summary>
        /// Общий метод вывода сообщений
        /// Гарантирует, что сообщения выведутся в порядке, определенном chunkIndex
        /// </summary>
        /// <remarks>
        /// Если сообщения идут последовательно (например, при однопоточном режиме или слабой пропускной способности),
        /// они будут выводиться сразу. Если сообщения приходят в разнобой, неподходящие для вывода будут сохраняться в буфер,
        /// а последний выведенный индекс будет сохраняться. При выводе нужного сообщения, буфер будет просматриваться на наличие следующих сообщений по списку
        /// </remarks>         
        /// <param name="chunkIndex"></param>
        /// <param name="hashBytes"></param>
        public void WriteToBuffer(int chunkIndex, byte[] hashBytes)
        {
            // Если мы видим, что пришел следующий по очереди блок, мы можем обойти буфер и вывести его сразу
            if (chunkIndex == lastChunkIndex + 1)
            {
                WriteHashToOutput(chunkIndex, hashBytes);
                lastChunkIndex = chunkIndex;
            }
            // Иначе - добавим в буфер и будем ждать подходящих сообщений
            else
            {
                lock (bufferLock)
                {
                    Buffer.Add(chunkIndex, hashBytes);
                }
            }

            CheckBufferForAvailableChunks();
        }

        private void CheckBufferForAvailableChunks()
        {
            lock (bufferLock)
            {
                var chunkIndexToSearch = lastChunkIndex + 1;

                while (Buffer.ContainsKey(chunkIndexToSearch))
                {
                    WriteHashToOutput(chunkIndexToSearch, Buffer[chunkIndexToSearch]);
                    Buffer.Remove(chunkIndexToSearch);
                    lastChunkIndex++;
                    chunkIndexToSearch++;
                }
            }
        }

        protected abstract void WriteHashToOutput(int chunkIndex, byte[] hashBytes);

        public virtual void Dispose()
        {
        }
    }
}

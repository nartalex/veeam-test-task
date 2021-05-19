using System;
using System.Collections.Generic;
using System.Linq;

namespace VeeamTestTask.Implementation.MultiThread
{
    public abstract class ResultWriter
    {
        private static Dictionary<int, byte[]> outputBuffer = null;
        private static readonly object creationLock = new object();
        private static readonly object bufferLock = new object();
        private static int lastChunkIndex = 0;

        protected ResultWriter()
        {
        }

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
                            outputBuffer = new(Environment.ProcessorCount * 2);
                        }
                    }
                }

                return outputBuffer;
            }
        }

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

        public void WriteToBuffer(int chunkIndex, byte[] hashBytes)
        {
            if (chunkIndex == lastChunkIndex + 1)
            {
                WriteHashToOutput(chunkIndex, hashBytes);
                lastChunkIndex = chunkIndex;
            }
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
                while (true)
                {
                    var chunkIndexToSearch = lastChunkIndex + 1;

                    if (Buffer.ContainsKey(chunkIndexToSearch))
                    {
                        WriteHashToOutput(chunkIndexToSearch, Buffer[chunkIndexToSearch]);
                        Buffer.Remove(chunkIndexToSearch);
                        lastChunkIndex++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        protected abstract void WriteHashToOutput(int chunkIndex, byte[] hashBytes);
    }
}

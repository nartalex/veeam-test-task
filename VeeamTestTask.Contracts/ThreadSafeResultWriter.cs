using System;
using System.Collections.Generic;
using System.Linq;

namespace VeeamTestTask.Contracts
{
    public abstract class ThreadSafeResultWriter : IDisposable
    {
        /// <summary>
        /// Буфер сообщений, сделан для вывода хэшей в строгой последовательности
        /// </summary>
        private static Dictionary<int, byte[]> _outputBuffer;
        private static readonly object _creationLock = new();
        private static readonly object _bufferLock = new();
        private static int _lastChunkIndex = 0;

        protected ThreadSafeResultWriter()
        {
        }

        /// <summary>
        /// Thread-safe singleton
        /// </summary>
        private static Dictionary<int, byte[]> Buffer
        {
            get
            {
                if (_outputBuffer == null)
                {
                    lock (_creationLock)
                    {
                        if (_outputBuffer == null)
                        {
                            // 16 элементов в буффере должно хватить для любого сценария поведения потоков
                            _outputBuffer = new(16);
                        }
                    }
                }

                return _outputBuffer;
            }
        }

        /// <summary>
        /// Флаг, показывающий наличие сообщений в буфере
        /// </summary>
        public static bool HasMessagesInBuffer
        {
            get
            {
                lock (_bufferLock)
                {
                    return Buffer.Count > 0;
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
            if (chunkIndex == _lastChunkIndex + 1)
            {
                WriteHashToOutput(chunkIndex, hashBytes);
                _lastChunkIndex = chunkIndex;
            }
            // Иначе - добавим в буфер и будем ждать подходящих сообщений
            else
            {
                lock (_bufferLock)
                {
                    Buffer.Add(chunkIndex, hashBytes);
                }
            }

            CheckBufferForAvailableChunks();
        }

        private void CheckBufferForAvailableChunks()
        {
            lock (_bufferLock)
            {
                for (var chunkIndexToSearch = _lastChunkIndex + 1; Buffer.ContainsKey(chunkIndexToSearch); chunkIndexToSearch++)
                {
                    WriteHashToOutput(chunkIndexToSearch, Buffer[chunkIndexToSearch]);
                    Buffer.Remove(chunkIndexToSearch);
                    _lastChunkIndex++;
                }
            }
        }

        protected abstract void WriteHashToOutput(int chunkIndex, byte[] hashBytes);

        public virtual void Dispose()
        {
        }
    }
}

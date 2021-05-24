using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.IO;
using VeeamTestTask.Implementation.MultiThread;
using VeeamTestTask.Implementation.SingleThread;

namespace VeeamTestTask.Benchmark
{
    public static class Program
    {
        public static void Main()
        {
#if DEBUG
            var benchmark = new BenchmarkClass
            {
                BlockSize = BenchmarkClass._4mbToBytes,
                InputFilePath = BenchmarkClass._32mbFilePath
            };

            benchmark.GlobalSetup();

            for (int i = 0; i < 10000; i++)
            {
                benchmark.IterationSetup();
                benchmark.MultiThreadImpl();

                if ((i % 10) == 0)
                {
                    Console.WriteLine($"Iteration {i} done");
                }
            }

            benchmark.GlobalCleanup();
#else
            try
            {
                var summary = BenchmarkRunner.Run<BenchmarkClass>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
#endif

            Console.ReadLine();
        }
    }

    [RPlotExporter]
    [MemoryDiagnoser]
    public class BenchmarkClass
    {
        private const string _hashAlgorithmName = "SHA256";

        [Params(_32mbFilePath, _1gbFilePath)]
        public string InputFilePath { get; set; }
        public const string _32kbFilePath = @"C:\32kb.bin";
        public const string _32mbFilePath = @"C:\32mb.bin";
        public const string _1gbFilePath = @"C:\1gb.bin";

        [Params(_4mbToBytes, _128mbToBytes)]
        public int BlockSize { get; set; }
        public const int _4kbToBytes = 4 * 1024;
        public const int _4mbToBytes = 4 * 1024 * 1024;
        public const int _128mbToBytes = 128 * 1024 * 1024;

        private Stream _fileStream;
        private VoidResultWriter _voidResultWriter;
        private SingleThreadChunkHashCalculator _singleThreadCalculator;
        private MultiThreadChunkHashCalculator _multiThreadCalculator;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _fileStream = new MemoryStream();
            _voidResultWriter = new VoidResultWriter();
            _singleThreadCalculator = new();
            _multiThreadCalculator = new();

            using var fileStream = new FileStream(InputFilePath, FileMode.Open, FileAccess.Read);
            fileStream.CopyTo(_fileStream);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
        }

        [Benchmark]
        public void SingleThreadImpl()
        {
            _singleThreadCalculator.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        [Benchmark]
        public void MultiThreadImpl()
        {
            _multiThreadCalculator.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _fileStream.Dispose();
        }
    }
}

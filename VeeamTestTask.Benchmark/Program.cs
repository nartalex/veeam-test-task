using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.IO;
using VeeamTestTask.Implementation.MultiThread;
using VeeamTestTask.Implementation.MultiThread2ndAttempt;
using VeeamTestTask.Implementation.MultiThread3rdAttempt;
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
                BlockSize = BenchmarkClass._4kbToBytes,
                InputFilePath = BenchmarkClass._32kbFilePath
            };

            benchmark.GlobalSetup();
            benchmark.IterationSetup();

            benchmark.SingleThreadImpl();

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
    [ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    public class BenchmarkClass
    {
        private const string _hashAlgorithmName = "SHA256";

        //[Params(_32kbFilePath, _32mbFilePath, _1gbFilePath)]
        [Params(_1gbFilePath)]
        public string InputFilePath { get; set; }
        public const string _32kbFilePath = @"C:\32kb.bin";
        public const string _32mbFilePath = @"C:\32mb.bin";
        public const string _1gbFilePath = @"C:\1gb.bin";

        [Params(_4kbToBytes, _4mbToBytes, _128mbToBytes)]
        public int BlockSize { get; set; }
        public const int _4kbToBytes = 4 * 1024;
        public const int _4mbToBytes = 4 * 1024 * 1024;
        public const int _128mbToBytes = 128 * 1024 * 1024;

        private Stream _fileStream;
        private VoidResultWriter _voidResultWriter;
        private SingleThreadChunkHashCalculator _singleThreadCalculator;
        private MultiThreadChunkHashCalculator _multiThreadCalculator1st;
        private MultiThreadChunkHashCalculator2ndAttempt _multiThreadCalculator2nd;
        private ProducerThreadFor3rdAttempt _multiThreadCalculator3rd;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _fileStream = new MemoryStream();
            _singleThreadCalculator = new();
            _multiThreadCalculator1st = new();
            _multiThreadCalculator2nd = new();
            _multiThreadCalculator3rd = new();

            using var fileStream = new FileStream(InputFilePath, FileMode.Open, FileAccess.Read);
            fileStream.CopyTo(_fileStream);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
            _voidResultWriter = new VoidResultWriter();
        }

        //[Benchmark]
        public void SingleThreadImpl()
        {
            _singleThreadCalculator.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        //[Benchmark]
        public void MulriThreadImpl1st()
        {
            _multiThreadCalculator1st.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        //[Benchmark]
        public void MulriThreadImpl2nd()
        {
            _multiThreadCalculator2nd.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        [Benchmark]
        public void MulriThreadImpl3rd()
        {
            _multiThreadCalculator3rd.SplitFileAndCalculateHashes(_fileStream, BlockSize, _hashAlgorithmName, _voidResultWriter);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _voidResultWriter.Dispose();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _fileStream.Dispose();
        }
    }
}

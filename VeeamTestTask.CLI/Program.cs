using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using VeeamTestTask.Contracts;
using VeeamTestTask.Implementation;
using VeeamTestTask.Implementation.MultiThread2ndAttempt;
using VeeamTestTask.Implementation.MultiThread3rdAttempt;
using VeeamTestTask.Implementation.SingleThread;

namespace VeeamTestTask.CLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (Debugger.IsAttached || args.Length == 0)
                {
                    Console.Write("Specify input file path: ");
                    var inputPath = Console.ReadLine();

                    Console.Write("Specify the block size in bytes: ");
                    var blockSizeString = Console.ReadLine();
                    var isValidInt = int.TryParse(blockSizeString, out var blockSize);

                    ValidateParamsAndExecute(inputPath, null, isValidInt ? blockSize : -1, false, "SHA256");
                }
                else
                {
                    var rootCommand = new RootCommand
                    {
                        new Option<string>(
                            alias: "--input-path",
                            description: "Specify input file path"),
                        new Option<string>(
                            alias: "--output-path",
                            description: "Specify output file path if you want it to be in file"),
                        new Option<int>(
                            alias: "--block-size",
                            description: "Specify the block size in bytes"),
                        new Option<bool>(
                            alias: "--single-thread",
                            description: "Computes hash in single thread"),
                        new Option<string>(
                            alias: "--hash-algorithm-name",
                            defaultValue: "SHA256",
                            description: "Hash algorithm that will be used to calculate hash"),
                    };

                    rootCommand.Description = "Console App to chunk file and calculate its' hashes";

                    rootCommand.Handler = CommandHandler.Create<string, string, int, bool, string>(ValidateParamsAndExecute);

                    rootCommand.Invoke(args);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

#if DEBUG
            Console.Write("Press Enter to continue: ");
            Console.ReadLine();
#endif
        }

        public static void ValidateParamsAndExecute(string inputPath, string outputPath, int blockSize, bool singleThread, string hashAlgorithmName)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("Input file path must be specified");
                return;
            }

            var inputFile = new FileInfo(inputPath);

            if (!inputFile.Exists)
            {
                Console.WriteLine("Specified file does not exist");
                return;
            }

            if (inputFile.Length == 0)
            {
                Console.WriteLine("Can not generate hash from empty file");
                return;
            }

            if (blockSize < 1 || blockSize > 2147483591)
            {
                Console.WriteLine("Block size must be between 1 and 2,147,483,591");
                return;
            }

            using (var hashToCheck = HashAlgorithm.Create(hashAlgorithmName))
            {
                if (hashToCheck == null)
                {
                    Console.WriteLine("Valid hash algorithm must be specified");
                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Input file path: {inputPath}");
            Console.WriteLine($"Block size: {blockSize} bytes");
            Console.WriteLine($"Multithreading: {(singleThread ? "disabled" : "enabled")}");
            Console.WriteLine($"Hash algorithm: {hashAlgorithmName}");
            Console.WriteLine();
            Console.WriteLine($"Number of blocks: {Math.Ceiling(inputFile.Length / (decimal)blockSize)}");
            Console.WriteLine();

            IChunkHashCalculator hashCalculator = singleThread ? new SingleThreadChunkHashCalculator() : new ProducerThreadFor3rdAttempt();
            using ThreadSafeResultWriter resultWriter = string.IsNullOrWhiteSpace(outputPath) ? new ConsoleResultWriter() : new FileResultWriter(outputPath);

            var startDateTime = DateTime.Now;

            hashCalculator.SplitFileAndCalculateHashes(inputPath, blockSize, hashAlgorithmName, resultWriter.WriteToBuffer);

            var finishDateTime = DateTime.Now;

            Console.WriteLine();
            Console.WriteLine($"Done. Took {Math.Round((finishDateTime - startDateTime).TotalSeconds)} s");
        }
    }
}

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using VeeamTestTask.Contracts;
using VeeamTestTask.Implementation.MultiThread;
using VeeamTestTask.Implementation.SingleThread;

namespace VeeamTestTask.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Console.Write("Specify input file path: ");
                var path = Console.ReadLine();

                Console.Write("Specify the block size in bytes: ");
                var blockSizeString = Console.ReadLine();
                var isValidInt = int.TryParse(blockSizeString, out var blockSize);

                ValidateParamsAndExecute(path, isValidInt ? blockSize : -1, true, "SHA256");
            }
            else
            {
                var rootCommand = new RootCommand
                {
                    new Option<string>(
                        alias: "--path",
                        description: "Specify input file path"),
                    new Option<int>(
                        alias: "--block-size",
                        description: "Specify the block size in bytes"),
                    new Option<bool>(
                        alias: "--single-thread",
                        description: "Computes hash in single thread"),
                    new Option<string>(
                        alias: "--hash-algorithm-name",
                        description: "Hash algorithm that will be used to calculate hash",
                        defaultValue: "SHA256"),
                };

                rootCommand.Description = "Console App to chunk file and calculate its' hashes";

                rootCommand.Handler = CommandHandler.Create<string, int, bool, string>(ValidateParamsAndExecute);

                // Parse the incoming args and invoke the handler
                rootCommand.Invoke(args);
            }
        }

        public static void ValidateParamsAndExecute(string path, int blockSize, bool singleThread, string hashAlgorithmName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("Input file path must be specified");
                return;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine("Specified file does not exist");
                return;
            }

            if (blockSize < 1)
            {
                Console.WriteLine("Block size must be greater that zero");
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
            Console.WriteLine($"Input file path: {path}");
            Console.WriteLine($"Block size: {blockSize} bytes");
            Console.WriteLine();

            IChunkHashCalculator hashCalculator = singleThread ? new SingleThreadChunkHashCalculator() : new MultiThreadChunkHashCalculator();
            var consoleOutputWriter = new ConsoleResultWriter();

            hashCalculator.SplitFileAndCalculateHashes(path, blockSize, hashAlgorithmName, consoleOutputWriter.WriteToBuffer);
        }
    }
}

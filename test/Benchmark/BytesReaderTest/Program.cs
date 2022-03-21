using BenchmarkDotNet.Running;
using System;

namespace BytesReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<OnceTest>();
            Console.ReadKey();
        }
    }
}

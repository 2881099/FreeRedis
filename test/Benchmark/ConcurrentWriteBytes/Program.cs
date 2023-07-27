using BenchmarkDotNet.Running;
using System;
using System.Text;

namespace ConcurrentWriteBytes
{
    class Program
    {
        static void Main(string[] args)
        {
           
            BenchmarkRunner.Run<EncodeAndWriteTest>();
            Console.ReadKey();

        }
    }
}

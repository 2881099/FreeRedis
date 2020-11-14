using BenchmarkDotNet.Running;
using System;
using System.Text;

namespace ConcurrentWriteBytes
{
    class Program
    {
        static void Main(string[] args)
        {
           
            BenchmarkRunner.Run<ByteWriteTest>();
            Console.ReadKey();

        }
    }
}

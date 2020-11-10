using BenchmarkDotNet.Running;
using System;

namespace PingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PingPongTest>();
            Console.ReadKey();
        }
    }
}

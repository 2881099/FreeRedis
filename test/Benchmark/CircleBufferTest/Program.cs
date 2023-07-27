using BenchmarkDotNet.Running;

namespace CircleBufferTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<CompareTest>();
            Console.ReadKey();
        }
    }
}
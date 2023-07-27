using BenchmarkDotNet.Running;
using System;

namespace IntrinsicsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //EncodingGetCountTest encodingGetCountTest = new EncodingGetCountTest();
            //encodingGetCountTest.Normal();
            //encodingGetCountTest.Optimic();
            BenchmarkRunner.Run<EncodingGetCountTest>();
            Console.ReadKey();
        }
    }
}

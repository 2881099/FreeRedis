﻿using BenchmarkDotNet.Running;
using System;
using System.Text;

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

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DispatcherTest
{
    [MemoryDiagnoser, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class Dispatcher
    {
        public readonly SingleNode Head;
        public Dispatcher()
        {
            Head = new SingleNode();
            Head.Next = new SingleNode();
        }

        [Benchmark]
        public void TestLoop()
        {
            var bytes = new SingleNode[10240];
            for (int i = 0; i < 10240; i+=1)
            {
                bytes[i] = new SingleNode();
            }
        }

        [Benchmark]
        public void TestConcurrent()
        {
            var bytes = new SingleNode[10240];
            Parallel.For(0, 10240, (index) => { bytes[index] = new SingleNode(); });
        }
    }


    public class SingleNode
    {
        public int Num;
        public bool Shut;
        public object test;
        public SingleNode Next;
    }
}

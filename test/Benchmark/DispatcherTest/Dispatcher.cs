using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
        public void TestSingleNode()
        {
            var first = Head;
            first = first.Next;
        }

        [Benchmark]
        public void TestThreadPool()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((state) => {  }));
        }
    }


    public class SingleNode
    {
        public SingleNode Next;
    }
}

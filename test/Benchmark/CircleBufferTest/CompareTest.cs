using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircleBufferTest
{

    /*
     * OutOfMemoryException!
BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects.
If your benchmark allocates memory and keeps it alive, you are creating a memory leak.
You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.
     */
    [MemoryDiagnoser, MarkdownExporter, RPlotExporter,GcConcurrent]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    //[ShortRunJob]
    public class CompareTest
    {
        private readonly CircleBuffer<double> _circleBuffer;
        private readonly Queue<double> _queue;
        private readonly NMSQueue<double> _nms_queue;
        private readonly ConcurrentQueue<double> _con_queue;
        public CompareTest()
        {
            _circleBuffer = new CircleBuffer<double>(10,4096);
            _nms_queue = new NMSQueue<double>(10240);
            _queue = new Queue<double>();
            _con_queue = new ConcurrentQueue<double>();
        }
        //[Benchmark(Description = "环形队列1")]
        //public void CircleBufferTest()
        //{
        //    _circleBuffer.Enqueue(3.1415926);
        //    var result = _circleBuffer.Dequeue();
        //}

        ////[Benchmark(Description = "环形队列2")]
        ////public void NMSQueueTest()
        ////{
        ////    _nms_queue.Enqueue(3.1415926);
        ////    var result = _nms_queue.Dequeue();
        ////}

        //[Benchmark(Description = "C#队列")]
        //public void ConcurrentQueueTest()
        //{
        //    _queue.Enqueue(3.1415926);
        //    var result = _queue.Dequeue();
        //}

        //[Benchmark(Description = "环形队列(单线程)")]
        //public void CircleBufferSingleTest()
        //{
        //    _circleBuffer.ConcurrentEnqueue(3.1415926);
        //    var result = _circleBuffer.ConcurrentDequeue();

        //}


        //[Benchmark(Description = "并发队列(单线程)")]
        //public void ConcurrentQueueSingleTest()
        //{
        //    _con_queue.Enqueue(3.1415926);
        //    _con_queue.TryDequeue(out var result);
        //}



        [Benchmark(Description = "环形缓冲区(并发测试)")]
        public void CircleBufferConTest()
        {
            var result = Parallel.For(0, 1000, i =>
            {
                _circleBuffer.ConcurrentEnqueue(3.1415926);
                var result = _circleBuffer.ConcurrentDequeue();
            });
            while (!result.IsCompleted) { }

        }


        [Benchmark(Description = "并发队列(并发测试)")]
        public void ConcurrentQueueConTest()
        {
            var result = Parallel.For(0, 1000, i =>
            {
                _con_queue.Enqueue(3.1415926);
                _con_queue.TryDequeue(out var result);
            });
            while (!result.IsCompleted) { }
        }
        //[IterationCleanup]
        //public void IterationCleanup()
        //{
        //    _queue.Clear();
        //}
    }
}

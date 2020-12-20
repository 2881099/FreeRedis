using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentWriteBytes
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class ConcurrentTest
    {
        public readonly CircleTaskBuffer<int> _newQueue;
        public readonly ConcurrentQueue<int> _queue;
        public readonly SourceConcurrentQueue<int> _sourceQueue;
        public const int Count = 1000;

        public ConcurrentTest()
        {
            _newQueue = new CircleTaskBuffer<int>();
            _queue = new ConcurrentQueue<int>();
            _sourceQueue = new SourceConcurrentQueue<int>();
        }

        //[Benchmark]
        //public void NewConcurrentQueue()
        //{
        //    Parallel.For(0, Count, (i) => {
        //        _newQueue.Enqueue(i);
        //    });
        //    Parallel.For(0, Count, (i) => {
        //        var _ = _newQueue.Dequeue();
        //    });
        //}

        [Benchmark]
        public void CircleTaskBuffer()
        {
            Parallel.For(0, Count, (i) => {
               
                _newQueue.WriteNext();
            });
            Parallel.For(0, Count, (i) => {
                _newQueue.ReadNext(i);
            });
        }

        private int _lock;
        //[Benchmark]
        //public void EmptyLock()
        //{
        //    Parallel.For(0, Count, (i) => {

        //        SpinWait wait = default;
        //        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        //        {
        //            wait.SpinOnce();
        //        }
        //        int a = 0;
        //        _lock = 0;
        //    });
        //    Parallel.For(0, Count, (i) => {

        //        SpinWait wait = default;
        //        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        //        {
        //            wait.SpinOnce();
        //        }
        //        int a = 0;
        //        _lock = 0;
        //    });

        //}

        [Benchmark]
        public void ConcurrentQueue()
        {
            Parallel.For(0, Count, (i) => {
                _queue.Enqueue(i);
            });
            Parallel.For(0, Count, (i) => {
                _queue.TryDequeue(out var _);
            });
        }

        //[Benchmark]
        //public void ConcurrentQueue2()
        //{
        //    Parallel.For(0, Count, (i) => {
        //        _sourceQueue.Enqueue(i);
        //    });
        //    Parallel.For(0, Count, (i) => {
        //        _sourceQueue.TryDequeue(out var _);
        //    });
        //}

    }

}

using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Threading;

namespace DispatcherTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<Dispatcher>();
            Stopwatch watch = new Stopwatch();
            SingleNode single = new SingleNode();
            single.Next = new SingleNode();

            watch.Start();
            for (int i = 0; i < 1000000000; i++)
            {
                var first = single;
                first = first.Next;
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            watch.Restart();
            for (int i = 0; i < 1000000000; i++)
            {
               var t = new WaitCallback((state) => { });
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}

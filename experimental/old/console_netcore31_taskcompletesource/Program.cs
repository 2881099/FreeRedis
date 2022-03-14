using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_taskcompletesource
{
    class Program
    {

        private static ManualResetValueTaskSource<int> mrvts;
        private static ConfiguredValueTaskAwaitable<int> _task;
        static void Main()
        {
            int testIndex = 0;
            ConcurrentDictionary<int,int> a = new ConcurrentDictionary<int,int>();
            Parallel.For(1, 1024000, (index) =>
            {
                int temp = Interlocked.Increment(ref testIndex);
                a[temp] = testIndex;
            });
            Console.WriteLine(a.Count);
            Console.WriteLine(testIndex);
            //mrvts = new ManualResetValueTaskSource<int>();

            //_task = new ValueTask<int>(mrvts, 0).ConfigureAwait(false);
            ////mrvts.OnCompleted(s => { Console.WriteLine(1); }, null, 2, ValueTaskSourceOnCompletedFlags.None);
            ////mrvts.Reset();
            ////mrvts.Reset();
            ////Console.WriteLine(mrvts.Version);

            ////mrvts.SetResult(42);

            ////Assert.Equal(ValueTaskSourceStatus.Succeeded, mrvts.GetStatus(2));
            ////Assert.Equal(42, mrvts.GetResult(2));
            ////Test();
            //Task.Run(() =>
            //{
            //    for (int i = 0; i < 10; i++)
            //    {
            //        Run();
            //        Thread.Sleep(3000);
            //        mrvts.SetResult(i);
            //        //mrvts.Reset();
            //        //mrvts.ve
            //    }
                
            //});
            //Assert.Equal(2, mrvts.Version);
            Console.ReadKey();
        }
        public static async void Run()
        {

            var result = await _task;
            Console.WriteLine(result);

        }
        public static async void Test()
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
           
            //await tcs.Task;
            Console.WriteLine(mrvts.Version);
        }

    }
}
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_taskcompletesource
{
    class Program
    {

        private static ManualResetValueTaskSource<int> mrvts;
        static void Main()
        {
            mrvts = new ManualResetValueTaskSource<int>();
            mrvts.OnCompleted(s => { Console.WriteLine(1); }, null, 2, ValueTaskSourceOnCompletedFlags.None);
            mrvts.Reset();
            mrvts.Reset();
            Console.WriteLine(mrvts.Version);

            mrvts.SetResult(42);

            //Assert.Equal(ValueTaskSourceStatus.Succeeded, mrvts.GetStatus(2));
            //Assert.Equal(42, mrvts.GetResult(2));
            Test();

            //Assert.Equal(2, mrvts.Version);
            Console.ReadKey();
        }

        public static async void Test()
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
           
            //await tcs.Task;
            Console.WriteLine(mrvts.Version);
        }

    }
}
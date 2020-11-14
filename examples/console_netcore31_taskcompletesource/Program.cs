using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_taskcompletesource
{
    class Program
    {
        private static ConcurrentQueue<TaskCompletionSource<string>> _taskQueue;
        static void Main()
        {
            Console.WriteLine(Encoding.UTF8.GetBytes("+").Length);
            _taskQueue = new ConcurrentQueue<TaskCompletionSource<string>>();
            var result = Call();
            Console.ReadKey();
        }

        public static async Task<string> Call()
        {
            Run();
            var result = await GetResultAsync();
            Console.WriteLine(result);
            result = await GetResultAsync();
            Console.WriteLine(result);
            result = await GetResultAsync();
            Console.WriteLine(result);
            return result;
        }

        public static async void Run()
        {

            await Task.Run(() =>
            {

                Thread.Sleep(3000);
                //Handler(Encoding.UTF8.GetBytes("+OK+10086+OK1").AsSpan());
                Handler(Encoding.UTF8.GetBytes("+OK+10086+OK1"));

            });
        }

        public static Task<string> GetResultAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            _taskQueue.Enqueue(tcs);
            return tcs.Task;
        }


        #region HandlerBytes
        private static void Handler(ReadOnlySequence<byte> sequence)
        {
            TaskCompletionSource<string> task;
            var reader = new SequenceReader<byte>(sequence);
            while (reader.TryReadTo(out var result, new byte[] { (byte)43 }))
            {
                //sequence = sequence.Slice(reader.Position);
                if (sequence.Length > 0)
                {
                    while (!_taskQueue.TryDequeue(out task)) { }
                    task.SetResult(Encoding.UTF8.GetString(result.ToArray()));
                }
            }

        }

        private static void Handler(ReadOnlySpan<byte> span)
        {

            TaskCompletionSource<string> task;
            var offset = span.IndexOf((byte)43);
            while (offset != -1)
            {
                if (offset != 0)
                {
                    while (!_taskQueue.TryDequeue(out task)) { }
                    task.SetResult(Encoding.UTF8.GetString(span.Slice(0, offset)));
                }
                span = span.Slice(offset + 1, span.Length - offset - 1);
                offset = span.IndexOf((byte)43);
            }
            while (!_taskQueue.TryDequeue(out task)) { }
            task.SetResult(Encoding.UTF8.GetString(span.Slice(0, span.Length)));

        }
        #endregion
    }
}

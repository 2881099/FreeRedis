using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    internal static class TaskEx
    {
        public static Task<T> FromResult<T>(T value)
        {
#if net40
            return new Task<T>(() => value);
#else
            return Task.FromResult(value);
#endif
        }
        public static Task Run(Action action)
        {
#if net40
            var tcs = new TaskCompletionSource<object>();
            new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            { IsBackground = true }.Start();
            return tcs.Task;
#else
            return Task.Run(action);
#endif
        }
        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            var tcs = new TaskCompletionSource<TResult>();
            new Thread(() =>
            {
                try
                {
                    tcs.SetResult(function());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            { IsBackground = true }.Start();
            return tcs.Task;
        }
        public static Task Delay(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            var timer = new System.Timers.Timer(timeout.TotalMilliseconds) { AutoReset = false };
            timer.Elapsed += delegate { timer.Dispose(); tcs.SetResult(null); };
            timer.Start();
            return tcs.Task;
        }

#if !NET40
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, string message = "The operation has timed out.")
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                }
                else
                {
                    throw new TimeoutException(message);
                }
            }
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout, string message = "The operation has timed out.")
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task;
                }
                else
                {
                    throw new TimeoutException(message);
                }
            }
        }
#endif
    }
}
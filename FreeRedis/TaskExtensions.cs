using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    public static class TaskExtensions
    {
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
    }
}

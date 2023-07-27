using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis.Engine.Standard
{
    abstract class RedisTaskBase
    {

    }
    internal class RedisTask
    {
        private static readonly Encoding _utf8;
        static RedisTask()
        {
            _utf8 = Encoding.UTF8;
        }

        public readonly TaskCompletionSource<long>? LongTask;
        public readonly TaskCompletionSource<string>? StringTask;
        public readonly TaskCompletionSource<bool>? BoolTask;
        public readonly Func<byte[], Task> HandleMethod;
        public readonly Func<byte[], Task> HandleError;
        internal RedisTask(Func<TaskCompletionSource<long>, byte[], Task> protocol) 
        {
            LongTask = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
            HandleMethod = async bytes => await protocol(LongTask,bytes);
            HandleError = bytes => LongTask.SetException(new Exception(_utf8.GetString(bytes));
        }
        internal RedisTask(Func<TaskCompletionSource<string>, byte[], Task> protocol)
        {
 
        }
        internal RedisTask(TaskCompletionSource<bool> task)
        {
            BoolTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        static RedisTask CreateLongTask()
        {
            return new RedisTask() { LongTask = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously) };
        }
        static RedisTask CreateStringTask()
        {
            return new RedisTask() { StringTask = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously) };
        }
        static RedisTask CreateBoolTask()
        {
            return new RedisTask() { BoolTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously) };
        }

        public async Task SetResult(byte[] bytes)
        {

        }
    }
}

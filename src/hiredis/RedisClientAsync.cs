#if isasync
using hiredis.Internal;
using hiredis.Internal.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;

namespace hiredis
{
    partial class RedisClient
    {
        //public Task<object> CallAsync(CommandPacket cmd) => Adapter.AdapterCallAsync(cmd, rt => rt.ThrowOrValue());
        protected Task<TValue> CallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse) => Adapter.AdapterCallAsync(cmd, parse);

        async internal Task<T> LogCallAsync<T>(CommandPacket cmd, Func<Task<T>> func)
        {
            if (this.Notice == null) return await func();
            Exception exception = null;
            Stopwatch sw = new Stopwatch();
            T ret = default(T);
            sw.Start();
            try
            {
                ret = await func();
                return ret;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                sw.Stop();
                LogCallFinally(cmd, ret, sw, exception);
            }
        }
    }
}
#endif
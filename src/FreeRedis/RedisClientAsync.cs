#if isasync
using FreeRedis.Internal;
using FreeRedis.Internal.ObjectPool;
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

namespace FreeRedis
{
    partial class RedisClient
    {
        public Task<object> CallAsync(CommandPacket cmd) => Adapter.AdapterCallAsync(cmd, rt => rt.ThrowOrValue());
        protected Task<TValue> CallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse) => Adapter.AdapterCallAsync(cmd, parse);

        async internal Task<T> LogCallAsync<T>(CommandPacket cmd, Func<Task<T>> func)
        {
            var isnotice = this.Notice != null;
            if (isnotice == false && this.Interceptors.Any() == false) return await func();
            Exception exception = null;
            Stopwatch sw = default;
            if (isnotice)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            T ret = default(T);
            var isnewval = false;
            var localInterceptors = this.Interceptors.Select(ctor =>
            {
                var intercepter = ctor?.Invoke();
                intercepter.Stopwatch.Start();
                intercepter.Before(cmd);
                if (intercepter.ValueIsChanged)
                {
                    isnewval = true;
                    ret = (T)intercepter.Value;
                }
                return intercepter;
            }).ToArray();

            try
            {
                if (isnewval == false) ret = await func();
                return ret;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                foreach (var interceptor in localInterceptors)
                {
                    interceptor.Value = ret;
                    interceptor.Exception = exception;
                    interceptor.Stopwatch.Stop();
                    interceptor.End(cmd);
                }

                if (isnotice)
                {
                    sw.Stop();
                    LogCallFinally(cmd, ret, sw, exception);
                }
            }
        }
    }
}
#endif
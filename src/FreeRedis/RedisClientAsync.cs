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
            cmd.Prefix(Prefix);
            var isnotice = this.Notice != null;
            if (isnotice == false && this.Interceptors.Any() == false) return await func();
            Exception exception = null;

            T ret = default(T);
            var isaopval = false;
            IInterceptor[] aops = new IInterceptor[this.Interceptors.Count + (isnotice ? 1 : 0)];
            Stopwatch[] aopsws = new Stopwatch[aops.Length];
            for (var idx = 0; idx < aops.Length; idx++)
            {
                aopsws[idx] = new Stopwatch();
                aopsws[idx].Start();
                aops[idx] = isnotice && idx == aops.Length - 1 ? new NoticeCallInterceptor(this) : this.Interceptors[idx]?.Invoke();
                var args = new InterceptorBeforeEventArgs(this, cmd, typeof(T));
                aops[idx].Before(args);
                if (args.ValueIsChanged && args.Value is T argsValue)
                {
                    isaopval = true;
                    ret = argsValue;
                }
            }
            try
            {
                if (isaopval == false) ret = await func();
                return ret;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                for (var idx = 0; idx < aops.Length; idx++)
                {
                    aopsws[idx].Stop();
                    var args = new InterceptorAfterEventArgs(this, cmd, typeof(T), ret, exception, aopsws[idx].ElapsedMilliseconds);
                    aops[idx].After(args);
                }
            }
        }
    }
}
#endif
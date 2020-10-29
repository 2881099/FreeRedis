#if net40
#else
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
        public Task<object> CallAsync(CommandPacket cmd) => Adapter.AdapaterCallAsync(cmd, rt => rt.ThrowOrValue());
        protected Task<TValue> CallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse) => Adapter.AdapaterCallAsync(cmd, parse);

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

        public Task<long> IncrByAsync(string key, long increment) => CallAsync("INCRBY".Input(key, increment).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task SetAsync<T>(string key, T value, int timeoutSeconds = 0) => CallAsync("SET"
            .Input(key)
            .InputRaw(SerializeRedisValue(value))
            .InputIf(timeoutSeconds >= 1, "EX", (long)timeoutSeconds)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<string> GetAsync(string key) => CallAsync("GET".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> GetAsync<T>(string key) => CallAsync("GET".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
    }
}
#endif
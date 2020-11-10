using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<bool> PfAddAsync(string key, params object[] elements) => CallAsync("PFADD".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<bool>());
        public Task<long> PfCountAsync(params string[] keys) => CallAsync("PFCOUNT".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task PfMergeAsync(string destkey, params string[] sourcekeys) => CallAsync("PFMERGE".InputKey(destkey).InputKey(sourcekeys), rt => rt.ThrowOrValue<string>());
        #endregion
#endif

        public bool PfAdd(string key, params object[] elements) => Call("PFADD".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<bool>());
        public long PfCount(params string[] keys) => Call("PFCOUNT".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public void PfMerge(string destkey, params string[] sourcekeys) => Call("PFMERGE".InputKey(destkey).InputKey(sourcekeys), rt => rt.ThrowOrValue<string>());
    }
}

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
        public Task<bool> PfAddAsync(string key, params object[] elements) => CallAsync("PFADD"
            .Input(key)
            .Input(elements.Select(a => SerializeRedisValue(a)).ToArray())
            .FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public Task<long> PfCountAsync(params string[] keys) => CallAsync("PFCOUNT".SubCommand(null)
            .InputIf(keys?.Any() == true, keys)
            .FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public Task PfMergeAsync(string destkey, params string[] sourcekeys) => CallAsync("PFMERGE"
            .Input(destkey)
            .InputIf(sourcekeys?.Any() == true, sourcekeys)
            .FlagKey(destkey)
            .FlagKey(sourcekeys), rt => rt.ThrowOrValue<string>());
        #endregion
#endif

        public bool PfAdd(string key, params object[] elements) => Call("PFADD"
            .Input(key)
            .Input(elements.Select(a => SerializeRedisValue(a)).ToArray())
            .FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public long PfCount(params string[] keys) => Call("PFCOUNT".SubCommand(null)
            .InputIf(keys?.Any() == true, keys)
            .FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public void PfMerge(string destkey, params string[] sourcekeys) => Call("PFMERGE"
            .Input(destkey)
            .InputIf(sourcekeys?.Any() == true, sourcekeys)
            .FlagKey(destkey)
            .FlagKey(sourcekeys), rt => rt.ThrowOrValue<string>());
    }
}

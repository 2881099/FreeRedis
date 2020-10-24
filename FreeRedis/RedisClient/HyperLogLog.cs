using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public bool PfAdd(string key, params object[] elements) => Call<bool>("PFADD"
            .Input(key)
            .Input(elements.Select(a => SerializeRedisValue(a)).ToArray())
            .FlagKey(key), rt => rt.ThrowOrValue());

        public long PfCount(params string[] keys) => Call<long>("PFCOUNT".SubCommand(null)
            .InputIf(keys?.Any() == true, keys)
            .FlagKey(keys), rt => rt.ThrowOrValue());

        public void PfMerge(string destkey, params string[] sourcekeys) => Call<string>("PFMERGE"
            .Input(destkey)
            .InputIf(sourcekeys?.Any() == true, sourcekeys)
            .FlagKey(destkey)
            .FlagKey(sourcekeys), rt => rt.ThrowOrValue());
    }
}

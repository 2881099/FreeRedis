using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace hiredis
{
    partial class RedisClient
    {
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

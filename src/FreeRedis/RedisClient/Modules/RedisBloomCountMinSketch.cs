using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string CmsInitByDim(string key, long width, long depth) => Call("CMS.INITBYDIM".Input(key, width, depth).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string CmsInitByProb(string key, decimal error, decimal probability) => Call("CMS.INITBYPROB".Input(key, error, probability).FlagKey(key), rt => rt.ThrowOrValue<string>());

        public long CmsIncrBy(string key, string item, long increment) => Call("CMS.INCRBY".Input(key, item, increment).FlagKey(key), rt => rt.ThrowOrValue(a => a.ConvertTo<long[]>().FirstOrDefault()));
        public long[] CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call("CMS.INCRBY".Input(key).InputKv(itemIncrements, SerializeRedisValue).FlagKey(key), rt => rt.ThrowOrValue<long[]>());

        public long[] CmsQuery(string key, string[] items) => Call("CMS.QUERY".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue<long[]>());
        public string CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call("CMS.MERGE"
            .Input(dest, numKeys)
            .Input(src)
            .InputIf(weights?.Any() == true, "WEIGHTS", weights)
            .FlagKey(dest)
            .FlagKey(src), rt => rt.ThrowOrValue<string>());

        public Dictionary<string, string> CmsInfo(string key) => Call("CMS.INFO".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

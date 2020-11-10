using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string CmsInitByDim(string key, long width, long depth) => Call("CMS.INITBYDIM".InputKey(key, width, depth), rt => rt.ThrowOrValue<string>());
        public string CmsInitByProb(string key, decimal error, decimal probability) => Call("CMS.INITBYPROB".InputKey(key, error, probability), rt => rt.ThrowOrValue<string>());

        public long CmsIncrBy(string key, string item, long increment) => Call("CMS.INCRBY".InputKey(key, item, increment), rt => rt.ThrowOrValue(a => a.ConvertTo<long[]>().FirstOrDefault()));
        public long[] CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call("CMS.INCRBY".InputKey(key).InputKv(itemIncrements, false, SerializeRedisValue), rt => rt.ThrowOrValue<long[]>());

        public long[] CmsQuery(string key, string[] items) => Call("CMS.QUERY".InputKey(key, items), rt => rt.ThrowOrValue<long[]>());
        public string CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call("CMS.MERGE"
            .InputKey(dest, numKeys)
            .InputKey(src)
            .InputIf(weights?.Any() == true, "WEIGHTS", weights), rt => rt.ThrowOrValue<string>());

        public Dictionary<string, string> CmsInfo(string key) => Call("CMS.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

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

        public Task<string> CmsInitByDimAsync(string key, long width, long depth) => CallAsync("CMS.INITBYDIM".InputKey(key, width, depth), rt => rt.ThrowOrValue<string>());
        public Task<string> CmsInitByProbAsync(string key, decimal error, decimal probability) => CallAsync("CMS.INITBYPROB".InputKey(key, error, probability), rt => rt.ThrowOrValue<string>());

        public Task<long> CmsIncrByAsync(string key, string item, long increment) => CallAsync("CMS.INCRBY".InputKey(key, item, increment), rt => rt.ThrowOrValue(a => a.ConvertTo<long[]>().FirstOrDefault()));
        public Task<long[]> CmsIncrByAsync(string key, Dictionary<string, long> itemIncrements) => CallAsync("CMS.INCRBY".InputKey(key).InputKv(itemIncrements, false, SerializeRedisValue), rt => rt.ThrowOrValue<long[]>());

        public Task<long[]> CmsQueryAsync(string key, string[] items) => CallAsync("CMS.QUERY".InputKey(key, items), rt => rt.ThrowOrValue<long[]>());
        public Task<string> CmsMergeAsync(string dest, long numKeys, string[] src, long[] weights) => CallAsync("CMS.MERGE"
            .InputKey(dest, numKeys)
            .InputKey(src)
            .InputIf(weights?.Any() == true, "WEIGHTS", weights), rt => rt.ThrowOrValue<string>());

        public Task<Dictionary<string, string>> CmsInfoAsync(string key) => CallAsync("CMS.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));

        #endregion
#endif

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

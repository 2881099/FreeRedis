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

        public Task<string> TopkReserveAsync(string key, long topk, long width, long depth, decimal decay) => CallAsync("TOPK.RESERVE".InputKey(key).Input(topk, width, depth, decay), rt => rt.ThrowOrValue<string>());
        public Task<string[]> TopkAddAsync(string key, string[] items) => CallAsync("TOPK.ADD".InputKey(key, items), rt => rt.ThrowOrValue<string[]>());

        public Task<string> TopkIncrByAsync(string key, string item, long increment) => CallAsync("TOPK.INCRBY".InputKey(key, item, increment), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<string>()));
        public Task<string[]> TopkIncrByAsync(string key, Dictionary<string, long> itemIncrements) => CallAsync("TOPK.INCRBY".InputKey(key).InputKv(itemIncrements, false, SerializeRedisValue), rt => rt.ThrowOrValue<string[]>());

        public Task<bool[]> TopkQueryAsync(string key, string[] items) => CallAsync("TOPK.QUERY".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());
        public Task<long[]> TopkCountAsync(string key, string[] items) => CallAsync("TOPK.COUNT".InputKey(key, items), rt => rt.ThrowOrValue<long[]>());

        public Task<string[]> TopkListAsync(string key) => CallAsync("TOPK.LIST".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<Dictionary<string, string>> TopkInfoAsync(string key) => CallAsync("TOPK.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));

        #endregion
#endif

        public string TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call("TOPK.RESERVE".InputKey(key).Input(topk, width, depth, decay), rt => rt.ThrowOrValue<string>());
        public string[] TopkAdd(string key, string[] items) => Call("TOPK.ADD".InputKey(key, items), rt => rt.ThrowOrValue<string[]>());

        public string TopkIncrBy(string key, string item, long increment) => Call("TOPK.INCRBY".InputKey(key, item, increment), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<string>()));
        public string[] TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call("TOPK.INCRBY".InputKey(key).InputKv(itemIncrements, false, SerializeRedisValue), rt => rt.ThrowOrValue<string[]>());

        public bool[] TopkQuery(string key, string[] items) => Call("TOPK.QUERY".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());
        public long[] TopkCount(string key, string[] items) => Call("TOPK.COUNT".InputKey(key, items), rt => rt.ThrowOrValue<long[]>());

        public string[] TopkList(string key) => Call("TOPK.LIST".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public Dictionary<string, string> TopkInfo(string key) => Call("TOPK.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

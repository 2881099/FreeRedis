using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
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

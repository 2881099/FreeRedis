using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call("TOPK.RESERVE".Input(key, topk, width, depth, decay).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string[] TopkAdd(string key, string[] items) => Call("TOPK.ADD".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue<string[]>());

        public string TopkIncrBy(string key, string item, long increment) => Call("TOPK.INCRBY".Input(key, item, increment).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<string>()));
        public string[] TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call("TOPK.INCRBY".Input(key).InputKv(itemIncrements).FlagKey(key), rt => rt.ThrowOrValue<string[]>());

        public bool[] TopkQuery(string key, string[] items) => Call("TOPK.QUERY".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue<bool[]>());
        public long[] TopkCount(string key, string[] items) => Call("TOPK.COUNT".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue<long[]>());

        public string[] TopkList(string key) => Call("TOPK.LIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Dictionary<string, string> TopkInfo(string key) => Call("TOPK.INFO".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string BfReserve(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => Call("BF.RESERVE"
            .InputKey(key)
            .Input(errorRate, capacity)
            .InputIf(expansion != 2, "EXPANSION", expansion)
            .InputIf(nonScaling, "NONSCALING"), rt => rt.ThrowOrValue<string>());
        public bool BfAdd(string key, string item) => Call("BF.ADD".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public bool[] BfMAdd(string key, string[] items) => Call("BF.MADD".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());

        public string BfInsert(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => Call("BF.INSERT"
            .InputKey(key)
            .InputIf(capacity != null, "CAPACITY", capacity)
            .InputIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
            .InputIf(expansion != 2, "EXPANSION", expansion)
            .InputIf(noCreate, "NOCREATE")
            .InputIf(nonScaling, "NONSCALING")
            .Input("ITEMS", items), rt => rt.ThrowOrValue<string>());

        public bool BfExists(string key, string item) => Call("BF.EXISTS".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public bool[] BfMExists(string key, string[] items) => Call("BF.MEXISTS".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());
        public ScanResult<byte[]> BfScanDump(string key, long iter) => Call("BF.SCANDUMP".InputKey(key, iter), rt => rt.ThrowOrValue((a, _) =>
            new ScanResult<byte[]>(a[0].ConvertTo<long>(), a[1].ConvertTo<byte[][]>())));

        public string BfLoadChunk(string key, long iter, byte[] data) => Call("BF.LOADCHUNK".InputKey(key, iter).InputRaw(data), rt => rt.ThrowOrValue<string>());
        public Dictionary<string, string> BfInfo(string key) => Call("BF.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

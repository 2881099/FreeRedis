using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => Call("CF.RESERVE"
            .Input(key, capacity)
            .InputIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
            .InputIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
            .InputIf(expansion != 2, "EXPANSION", expansion)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public bool CfAdd(string key, string item) => Call("CF.ADD".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool CfAddNx(string key, string item) => Call("CF.ADDNX".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public string CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
        public string CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
        string CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => Call((nx ? "CF.INSERTNX" : "CF.INSERT")
            .Input(key)
            .InputIf(capacity != null, "CAPACITY", capacity)
            .InputIf(noCreate, "NOCREATE")
            .Input("ITEMS", items)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public bool CfExists(string key, string item) => Call("CF.EXISTS".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool CfDel(string key, string item) => Call("CF.DEL".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public long CfCount(string key, string item) => Call("CF.COUNT".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public ScanResult<byte[]> CfScanDump(string key, long iter) => Call("CF.SCANDUMP".Input(key, iter).FlagKey(key), rt => rt.ThrowOrValue((a, _) =>
            new ScanResult<byte[]>(a[0].ConvertTo<long>(), a[1].ConvertTo<byte[][]>())));

        public string CfLoadChunk(string key, long iter, byte[] data) => Call("CF.LOADCHUNK".Input(key, iter).InputRaw(data).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Dictionary<string, string> CfInfo(string key) => Call("CF.INFO".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

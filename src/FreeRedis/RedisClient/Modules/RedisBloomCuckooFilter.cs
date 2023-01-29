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

        public Task<string> CfReserveAsync(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => CallAsync("CF.RESERVE"
            .InputKey(key, capacity)
            .InputIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
            .InputIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
            .InputIf(expansion != 2, "EXPANSION", expansion), rt => rt.ThrowOrValue<string>());

        public Task<bool> CfAddAsync(string key, string item) => CallAsync("CF.ADD".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public Task<bool> CfAddNxAsync(string key, string item) => CallAsync("CF.ADDNX".InputKey(key, item), rt => rt.ThrowOrValue<bool>());

        async public Task<string> CfInsertAsync(string key, string[] items, long? capacity = null, bool noCreate = false) => await CfInsertAsync(false, key, items, capacity, noCreate);
        async public Task<string> CfInsertNxAsync(string key, string[] items, long? capacity = null, bool noCreate = false) => await CfInsertAsync(true, key, items, capacity, noCreate);
        Task<string> CfInsertAsync(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => CallAsync((nx ? "CF.INSERTNX" : "CF.INSERT")
            .InputKey(key)
            .InputIf(capacity != null, "CAPACITY", capacity)
            .InputIf(noCreate, "NOCREATE")
            .Input("ITEMS", items), rt => rt.ThrowOrValue<string>());

        public Task<bool> CfExistsAsync(string key, string item) => CallAsync("CF.EXISTS".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public Task<bool> CfDelAsync(string key, string item) => CallAsync("CF.DEL".InputKey(key, item), rt => rt.ThrowOrValue<bool>());

        public Task<long> CfCountAsync(string key, string item) => CallAsync("CF.COUNT".InputKey(key, item), rt => rt.ThrowOrValue<long>());

        public Task<ScanResult<byte[]>> CfScanDumpAsync(string key, long iter) => CallAsync("CF.SCANDUMP".InputKey(key, iter), rt => rt.ThrowOrValue((a, _) =>
            new ScanResult<byte[]>(a[0].ConvertTo<long>(), a[1].ConvertTo<byte[][]>())));

        public Task<string> CfLoadChunkAsync(string key, long iter, byte[] data) => CallAsync("CF.LOADCHUNK".InputKey(key, iter).InputRaw(data), rt => rt.ThrowOrValue<string>());
        public Task<Dictionary<string, string>> CfInfoAsync(string key) => CallAsync("CF.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));

        #endregion
#endif

        public string CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => Call("CF.RESERVE"
            .InputKey(key, capacity)
            .InputIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
            .InputIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
            .InputIf(expansion != 2, "EXPANSION", expansion), rt => rt.ThrowOrValue<string>());

        public bool CfAdd(string key, string item) => Call("CF.ADD".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public bool CfAddNx(string key, string item) => Call("CF.ADDNX".InputKey(key, item), rt => rt.ThrowOrValue<bool>());

        public string CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
        public string CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
        string CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => Call((nx ? "CF.INSERTNX" : "CF.INSERT")
            .InputKey(key)
            .InputIf(capacity != null, "CAPACITY", capacity)
            .InputIf(noCreate, "NOCREATE")
            .Input("ITEMS", items), rt => rt.ThrowOrValue<string>());

        public bool CfExists(string key, string item) => Call("CF.EXISTS".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public bool CfDel(string key, string item) => Call("CF.DEL".InputKey(key, item), rt => rt.ThrowOrValue<bool>());

        public long CfCount(string key, string item) => Call("CF.COUNT".InputKey(key, item), rt => rt.ThrowOrValue<long>());

        public ScanResult<byte[]> CfScanDump(string key, long iter) => Call("CF.SCANDUMP".InputKey(key, iter), rt => rt.ThrowOrValue((a, _) =>
            new ScanResult<byte[]>(a[0].ConvertTo<long>(), a[1].ConvertTo<byte[][]>())));

        public string CfLoadChunk(string key, long iter, byte[] data) => Call("CF.LOADCHUNK".InputKey(key, iter).InputRaw(data), rt => rt.ThrowOrValue<string>());
        public Dictionary<string, string> CfInfo(string key) => Call("CF.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
    }
}

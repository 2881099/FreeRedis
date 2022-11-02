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

        public Task<string> BfReserveAsync(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => CallAsync("BF.RESERVE"
            .InputKey(key)
            .Input(errorRate, capacity)
            .InputIf(expansion != 2, "EXPANSION", expansion)
            .InputIf(nonScaling, "NONSCALING"), rt => rt.ThrowOrValue<string>());
        public Task<bool> BfAddAsync(string key, string item) => CallAsync("BF.ADD".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public Task<bool[]> BfMAddAsync(string key, string[] items) => CallAsync("BF.MADD".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());

        public Task<string> BfInsertAsync(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => CallAsync("BF.INSERT"
            .InputKey(key)
            .InputIf(capacity != null, "CAPACITY", capacity)
            .InputIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
            .InputIf(expansion != 2, "EXPANSION", expansion)
            .InputIf(noCreate, "NOCREATE")
            .InputIf(nonScaling, "NONSCALING")
            .Input("ITEMS", items), rt => rt.ThrowOrValue<string>());

        public Task<bool> BfExistsAsync(string key, string item) => CallAsync("BF.EXISTS".InputKey(key, item), rt => rt.ThrowOrValue<bool>());
        public Task<bool[]> BfMExistsAsync(string key, string[] items) => CallAsync("BF.MEXISTS".InputKey(key, items), rt => rt.ThrowOrValue<bool[]>());
        public Task<ScanResult<byte[]>> BfScanDumpAsync(string key, long iter) => CallAsync("BF.SCANDUMP".InputKey(key, iter), rt => rt.ThrowOrValue((a, _) =>
            new ScanResult<byte[]>(a[0].ConvertTo<long>(), a[1].ConvertTo<byte[][]>())));

        public Task<string> BfLoadChunkAsync(string key, long iter, byte[] data) => CallAsync("BF.LOADCHUNK".InputKey(key, iter).InputRaw(data), rt => rt.ThrowOrValue<string>());
        public Task<Dictionary<string, string>> BfInfoAsync(string key) => CallAsync("BF.INFO".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));

        #endregion
#endif

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

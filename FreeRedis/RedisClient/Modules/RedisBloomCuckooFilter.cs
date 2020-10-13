using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => Call<string>("CF.RESERVE"
			.Input(key, capacity)
			.InputIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
			.InputIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
			.InputIf(expansion != 2, "EXPANSION", expansion)
			.FlagKey(key));
		public RedisResult<bool> CfAdd(string key, string item) => Call<bool>("CF.ADD".Input(key, item).FlagKey(key));
		public RedisResult<bool> CfAddNx(string key, string item) => Call<bool>("CF.ADDNX".Input(key, item).FlagKey(key));
		public RedisResult<string> CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
		public RedisResult<string> CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
		RedisResult<string> CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => Call<string>((nx ? "CF.INSERTNX" : "CF.INSERT")
			.Input(key)
			.InputIf(capacity != null, "CAPACITY", capacity)
			.InputIf(noCreate, "NOCREATE")
			.Input("ITEMS", items)
			.FlagKey(key));
		public RedisResult<bool> CfExists(string key, string item) => Call<bool>("CF.EXISTS".Input(key, item).FlagKey(key));
		public RedisResult<bool> CfDel(string key, string item) => Call<bool>("CF.DEL".Input(key, item).FlagKey(key));
		public RedisResult<long> CfCount(string key, string item) => Call<long>("CF.COUNT".Input(key, item).FlagKey(key));
		public RedisResult<ScanValue<byte[]>> CfScanDump(string key, long iter) => Call<object>("CF.SCANDUMP".Input(key, iter).FlagKey(key))
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> CfLoadChunk(string key, long iter, byte[] data) => Call<string>("CF.LOADCHUNK".Input(key, iter).InputRaw(data).FlagKey(key));
		public RedisResult<Dictionary<string, string>> CfInfo(string key) => Call<string[]>("CF.INFO".Input(key).FlagKey(key)).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

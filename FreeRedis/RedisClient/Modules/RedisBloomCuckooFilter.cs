using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => Call<string>("CF.RESERVE", key, ""
			.AddIf(true, capacity)
			.AddIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
			.AddIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.ToArray());
		public RedisResult<bool> CfAdd(string key, string item) => Call<bool>("CF.ADD", key, item);
		public RedisResult<bool> CfAddNx(string key, string item) => Call<bool>("CF.ADDNX", key, item);
		public RedisResult<string> CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
		public RedisResult<string> CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
		RedisResult<string> CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => Call<string>(nx ? "CF.INSERTNX" : "CF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> CfExists(string key, string item) => Call<bool>("CF.EXISTS", key, item);
		public RedisResult<bool> CfDel(string key, string item) => Call<bool>("CF.DEL", key, item);
		public RedisResult<long> CfCount(string key, string item) => Call<long>("CF.COUNT", key, item);
		public RedisResult<ScanValue<byte[]>> CfScanDump(string key, long iter) => Call<object>("CF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> CfLoadChunk(string key, long iter, byte[] data) => Call<string>("CF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> CfInfo(string key) => Call<string[]>("CF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> BfReserve(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => Call<string>("BF.RESERVE", key, ""
			.AddIf(true, errorRate, capacity)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(nonScaling, "NONSCALING")
			.ToArray());
		public RedisResult<bool> BfAdd(string key, string item) => Call<bool>("BF.ADD", key, item);
		public RedisResult<bool[]> BfMAdd(string key, string[] items) => Call<bool[]>("BF.MADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> BfInsert(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => Call<string>("BF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(nonScaling, "NONSCALING")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> BfExists(string key, string item) => Call<bool>("BF.EXISTS", key, item);
		public RedisResult<bool[]> BfMExists(string key, string[] items) => Call<bool[]>("BF.MEXISTS", key, "".AddIf(true, items).ToArray());
		public RedisResult<ScanValue<byte[]>> BfScanDump(string key, long iter) => Call<object>("BF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> BfLoadChunk(string key, long iter, byte[] data) => Call<string>("BF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> BfInfo(string key) => Call<string[]>("BF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public string BfReserve(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => Call<string>("BF.RESERVE"
			.Input(key)
			.Input(errorRate, capacity)
			.InputIf(expansion != 2, "EXPANSION", expansion)
			.InputIf(nonScaling, "NONSCALING")
			.FlagKey(key), rt => rt.ThrowOrValue());
		public bool BfAdd(string key, string item) => Call<bool>("BF.ADD".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue());
		public bool[] BfMAdd(string key, string[] items) => Call<bool[]>("BF.MADD".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public string BfInsert(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => Call<string>("BF.INSERT"
			.Input(key)
			.InputIf(capacity != null, "CAPACITY", capacity)
			.InputIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
			.InputIf(expansion != 2, "EXPANSION", expansion)
			.InputIf(noCreate, "NOCREATE")
			.InputIf(nonScaling, "NONSCALING")
			.Input("ITEMS", items)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public bool BfExists(string key, string item) => Call<bool>("BF.EXISTS".Input(key, item).FlagKey(key), rt => rt.ThrowOrValue());
		public bool[] BfMExists(string key, string[] items) => Call<bool[]>("BF.MEXISTS".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public ScanValue<byte[]> BfScanDump(string key, long iter) => Call<object, ScanValue<byte[]>>("BF.SCANDUMP".Input(key, iter).FlagKey(key), rt => rt.NewValue(a =>
		{
			var arr = a as List<object>;
			return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
		}).ThrowOrValue());
		public string BfLoadChunk(string key, long iter, byte[] data) => Call<string>("BF.LOADCHUNK".Input(key, iter).InputRaw(data).FlagKey(key), rt => rt.ThrowOrValue());
		public Dictionary<string, string> BfInfo(string key) => Call<string[], Dictionary<string, string>>("BF.INFO".Input(key).FlagKey(key), rt => rt.NewValue(a => a.MapToHash<string>(Encoding)).ThrowOrValue());
    }
}

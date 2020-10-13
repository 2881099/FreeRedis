using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public string TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call<string>("TOPK.RESERVE".Input(key, topk, width, depth, decay).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] TopkAdd(string key, string[] items) => Call<string[]>("TOPK.ADD".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public string TopkIncrBy(string key, string item, long increment) => Call<string[], string>("TOPK.INCRBY".Input(key, item, increment).FlagKey(key), rt => rt.NewValue(a => a.FirstOrDefault()).ThrowOrValue());
		public string[] TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<string[]>("TOPK.INCRBY".Input(key).InputKv(itemIncrements).FlagKey(key), rt => rt.ThrowOrValue());
		public bool[] TopkQuery(string key, string[] items) => Call<bool[]>("TOPK.QUERY".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public long[] TopkCount(string key, string[] items) => Call<long[]>("TOPK.COUNT".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] TopkList(string key) => Call<string[]>("TOPK.LIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public Dictionary<string, string> TopkInfo(string key) => Call<string[], Dictionary<string, string>>("TOPK.INFO".Input(key).FlagKey(key), rt => rt.NewValue(a => a.MapToHash<string>(Encoding)).ThrowOrValue());
    }
}

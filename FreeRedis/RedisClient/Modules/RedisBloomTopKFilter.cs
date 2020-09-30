using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call<string>("TOPK.RESERVE", key, topk, width, depth, decay);
		public RedisResult<string[]> TopkAdd(string key, string[] items) => Call<string[]>("TOPK.ADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> TopkIncrBy(string key, string item, long increment) => Call<string[]>("TOPK.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<string[]>("TOPK.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<bool[]> TopkQuery(string key, string[] items) => Call<bool[]>("TOPK.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<long[]> TopkCount(string key, string[] items) => Call<long[]>("TOPK.COUNT", key, "".AddIf(true, items).ToArray());
		public RedisResult<string[]> TopkList(string key) => Call<string[]>("TOPK.LIST", key);
		public RedisResult<Dictionary<string, string>> TopkInfo(string key) => Call<string[]>("TOPK.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call<string>("TOPK.RESERVE".Input(key, topk, width, depth, decay).FlagKey(key));
		public RedisResult<string[]> TopkAdd(string key, string[] items) => Call<string[]>("TOPK.ADD".Input(key).Input(items).FlagKey(key));
		public RedisResult<string> TopkIncrBy(string key, string item, long increment) => Call<string[]>("TOPK.INCRBY".Input(key, item, increment).FlagKey(key)).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<string[]>("TOPK.INCRBY".Input(key).InputKv(itemIncrements).FlagKey(key));
		public RedisResult<bool[]> TopkQuery(string key, string[] items) => Call<bool[]>("TOPK.QUERY".Input(key).Input(items).FlagKey(key));
		public RedisResult<long[]> TopkCount(string key, string[] items) => Call<long[]>("TOPK.COUNT".Input(key).Input(items).FlagKey(key));
		public RedisResult<string[]> TopkList(string key) => Call<string[]>("TOPK.LIST".Input(key).FlagKey(key));
		public RedisResult<Dictionary<string, string>> TopkInfo(string key) => Call<string[]>("TOPK.INFO".Input(key).FlagKey(key)).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> CmsInitByDim(string key, long width, long depth) => Call<string>("CMS.INITBYDIM".Input(key, width, depth).FlagKey(key));
		public RedisResult<string> CmsInitByProb(string key, decimal error, decimal probability) => Call<string>("CMS.INITBYPROB".Input(key, error, probability).FlagKey(key));
		public RedisResult<long> CmsIncrBy(string key, string item, long increment) => Call<long[]>("CMS.INCRBY".Input(key, item, increment).FlagKey(key)).NewValue(a => a.FirstOrDefault());
		public RedisResult<long[]> CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<long[]>("CMS.INCRBY".Input(key).InputKv(itemIncrements).FlagKey(key));
		public RedisResult<long[]> CmsQuery(string key, string[] items) => Call<long[]>("CMS.QUERY".Input(key).Input(items).FlagKey(key));
		public RedisResult<string> CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call<string>("CMS.MERGE"
			.Input(dest, numKeys)
			.Input(src)
			.InputIf(weights?.Any() == true, "WEIGHTS", weights)
			.FlagKey(dest)
			.FlagKey(src));
		public RedisResult<Dictionary<string, string>> CmsInfo(string key) => Call<string[]>("CMS.INFO".Input(key).FlagKey(key)).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

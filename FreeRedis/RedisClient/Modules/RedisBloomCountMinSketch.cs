using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> CmsInitByDim(string key, long width, long depth) => Call<string>("CMS.INITBYDIM", key, width, depth);
		public RedisResult<string> CmsInitByProb(string key, decimal error, decimal probability) => Call<string>("CMS.INITBYPROB", key, error, probability);
		public RedisResult<long> CmsIncrBy(string key, string item, long increment) => Call<long[]>("CMS.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<long[]> CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<long[]>("CMS.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<long[]> CmsQuery(string key, string[] items) => Call<long[]>("CMS.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call<string>("CMS.MERGE", null, ""
			.AddIf(true, dest, numKeys, src)
			.AddIf(weights?.Any() == true, "WEIGHTS", weights)
			.ToArray());
		public RedisResult<Dictionary<string, string>> CmsInfo(string key) => Call<string[]>("CMS.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
    }
}

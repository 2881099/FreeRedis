using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public string CmsInitByDim(string key, long width, long depth) => Call<string>("CMS.INITBYDIM".Input(key, width, depth).FlagKey(key), rt => rt.ThrowOrValue());
		public string CmsInitByProb(string key, decimal error, decimal probability) => Call<string>("CMS.INITBYPROB".Input(key, error, probability).FlagKey(key), rt => rt.ThrowOrValue());
		public long CmsIncrBy(string key, string item, long increment) => Call<long[], long>("CMS.INCRBY".Input(key, item, increment).FlagKey(key), rt => rt.NewValue(a => a.FirstOrDefault()).ThrowOrValue());
		public long[] CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<long[]>("CMS.INCRBY".Input(key).InputKv(itemIncrements).FlagKey(key), rt => rt.ThrowOrValue());
		public long[] CmsQuery(string key, string[] items) => Call<long[]>("CMS.QUERY".Input(key).Input(items).FlagKey(key), rt => rt.ThrowOrValue());
		public string CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call<string>("CMS.MERGE"
			.Input(dest, numKeys)
			.Input(src)
			.InputIf(weights?.Any() == true, "WEIGHTS", weights)
			.FlagKey(dest)
			.FlagKey(src), rt => rt.ThrowOrValue());
		public Dictionary<string, string> CmsInfo(string key) => Call<string[], Dictionary<string, string>>("CMS.INFO".Input(key).FlagKey(key), rt => rt.NewValue(a => a.MapToHash<string>(Encoding)).ThrowOrValue());
    }
}

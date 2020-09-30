using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<bool> PfAdd(string key, params string[] elements) => Call<bool>("PFADD", key, elements);
		public RedisResult<string[]> PfCount(string[] keys) => Call<string[]>("PFCOUNT", null, "".AddIf(keys?.Any() == true, keys).ToArray());
		public RedisResult<string> PfMerge(string destkey, params string[] sourcekeys) => Call<string>("PFMERGE", destkey, "".AddIf(sourcekeys?.Any() == true, sourcekeys).ToArray());
    }
}

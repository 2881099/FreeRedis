using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<bool> PfAdd(string key, params string[] elements) => Call<bool>("PFADD"
			.Input(key)
			.Input(elements)
			.FlagKey(key));
		public RedisResult<string[]> PfCount(string[] keys) => Call<string[]>("PFCOUNT".SubCommand(null)
			.InputIf(keys?.Any() == true, keys)
			.FlagKey(keys));
		public RedisResult<string> PfMerge(string destkey, params string[] sourcekeys) => Call<string>("PFMERGE"
			.Input(destkey)
			.InputIf(sourcekeys?.Any() == true, sourcekeys)
			.FlagKey(destkey)
			.FlagKey(sourcekeys));
    }
}

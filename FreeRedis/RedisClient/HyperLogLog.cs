using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public bool PfAdd(string key, params string[] elements) => Call<bool>("PFADD"
			.Input(key)
			.Input(elements)
			.FlagKey(key)).ThrowOrValue();

		public long PfCount(string[] keys) => Call<long>("PFCOUNT".SubCommand(null)
			.InputIf(keys?.Any() == true, keys)
			.FlagKey(keys)).ThrowOrValue();

		public void PfMerge(string destkey, params string[] sourcekeys) => Call<string>("PFMERGE"
			.Input(destkey)
			.InputIf(sourcekeys?.Any() == true, sourcekeys)
			.FlagKey(destkey)
			.FlagKey(sourcekeys)).ThrowOrValue();
	}
}

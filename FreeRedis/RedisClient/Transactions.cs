using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> Discard() => Call<string>("DISCARD");
		public RedisResult<object[]> Exec() => Call<object[]>("EXEC");
		public RedisResult<string> Multi() => Call<string>("MULTI");
		public RedisResult<string> UnWatch() => Call<string>("UNWATCH");
		public RedisResult<string> Watch(params string[] keys) => Call<string>("WATCH", null, keys);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<object> Eval(string script, string[] keys, params object[] arguments) => Call<object>("EVAL", null, script.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<object> EvalSha(string sha1, string[] keys, params object[] arguments) => Call<object>("EVALSHA", null, sha1.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<string> ScriptDebug(ScriptDebugOption options) => Call<string>("SCRIPT", "DEBUG", options);
		public RedisResult<bool> ScriptExists(string sha1) => Call<bool[]>("SCRIPT", "EXISTS", sha1).NewValue(a => a.FirstOrDefault());
		public RedisResult<bool[]> ScriptExists(string[] sha1) => Call<bool[]>("SCRIPT", "EXISTS", sha1);
		public RedisResult<string> ScriptFlush() => Call<string>("SCRIPT", "FLUSH");
		public RedisResult<string> ScriptKill() => Call<string>("SCRIPT", "KILL");
		public RedisResult<string> ScriptLoad(string script) => Call<string>("SCRIPT", "LOAD", script);
    }
}

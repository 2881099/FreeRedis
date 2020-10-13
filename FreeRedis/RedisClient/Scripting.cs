using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<object> Eval(string script, string[] keys, params object[] arguments) => Call<object>("EVAL"
			.Input(script, keys.Length)
			.Input(keys)
			.Input(arguments)
			.FlagKey(keys));
		public RedisResult<object> EvalSha(string sha1, string[] keys, params object[] arguments) => Call<object>("EVALSHA"
			.Input(sha1, keys.Length)
			.Input(keys)
			.Input(arguments)
			.FlagKey(keys));
		public RedisResult<string> ScriptDebug(ScriptDebugOption options) => Call<string>("SCRIPT".SubCommand("DEBUG").InputRaw(options));
		public RedisResult<bool> ScriptExists(string sha1) => Call<bool[]>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1)).NewValue(a => a.FirstOrDefault());
		public RedisResult<bool[]> ScriptExists(string[] sha1) => Call<bool[]>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1));
		public RedisResult<string> ScriptFlush() => Call<string>("SCRIPT".SubCommand("FLUSH"));
		public RedisResult<string> ScriptKill() => Call<string>("SCRIPT".SubCommand("KILL"));
		public RedisResult<string> ScriptLoad(string script) => Call<string>("SCRIPT".SubCommand("LOAD").InputRaw(script));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public object Eval(string script, string[] keys, params object[] arguments) => Call<object>("EVAL"
			.Input(script, keys.Length)
			.Input(keys)
			.Input(arguments)
			.FlagKey(keys), rt => rt.ThrowOrValue());

		public object EvalSha(string sha1, string[] keys, params object[] arguments) => Call<object>("EVALSHA"
			.Input(sha1, keys.Length)
			.Input(keys)
			.Input(arguments)
			.FlagKey(keys), rt => rt.ThrowOrValue());

		public void ScriptDebug(ScriptDebugOption options) => Call<string>("SCRIPT".SubCommand("DEBUG").InputRaw(options), rt => rt.ThrowOrValue());
		public bool ScriptExists(string sha1) => Call<bool[], bool>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1), rt => rt.NewValue(a => a.FirstOrDefault()).ThrowOrValue());
		public bool[] ScriptExists(string[] sha1) => Call<bool[]>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1), rt => rt.ThrowOrValue());

		public void ScriptFlush() => Call<string>("SCRIPT".SubCommand("FLUSH"), rt => rt.ThrowOrValue());
		public void ScriptKill() => Call<string>("SCRIPT".SubCommand("KILL"), rt => rt.ThrowOrValue());
		public string ScriptLoad(string script) => Call<string>("SCRIPT".SubCommand("LOAD").InputRaw(script), rt => rt.ThrowOrValue());
	}
}

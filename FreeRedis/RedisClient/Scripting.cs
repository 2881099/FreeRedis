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
			.FlagKey(keys)).ThrowOrValue();

		public object EvalSha(string sha1, string[] keys, params object[] arguments) => Call<object>("EVALSHA"
			.Input(sha1, keys.Length)
			.Input(keys)
			.Input(arguments)
			.FlagKey(keys)).ThrowOrValue();

		public void ScriptDebug(ScriptDebugOption options) => Call<string>("SCRIPT".SubCommand("DEBUG").InputRaw(options)).ThrowOrValue();
		public bool ScriptExists(string sha1) => Call<bool[]>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1)).NewValue(a => a.FirstOrDefault()).ThrowOrValue();
		public bool[] ScriptExists(string[] sha1) => Call<bool[]>("SCRIPT".SubCommand("EXISTS").InputRaw(sha1)).ThrowOrValue();

		public void ScriptFlush() => Call<string>("SCRIPT".SubCommand("FLUSH")).ThrowOrValue();
		public void ScriptKill() => Call<string>("SCRIPT".SubCommand("KILL")).ThrowOrValue();
		public string ScriptLoad(string script) => Call<string>("SCRIPT".SubCommand("LOAD").InputRaw(script)).ThrowOrValue();
	}
}

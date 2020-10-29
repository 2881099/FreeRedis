using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace hiredis
{
    partial class RedisClient
    {
        public object Eval(string script, string[] keys = null, params object[] arguments) => Call("EVAL"
            .Input(script, keys?.Length ?? 0)
            .InputIf(keys?.Any() == true, keys)
            .Input(arguments)
            .FlagKey(keys), rt => rt.ThrowOrValue());

        public object EvalSha(string sha1, string[] keys = null, params object[] arguments) => Call("EVALSHA"
            .Input(sha1, keys?.Length ?? 0)
            .InputIf(keys?.Any() == true, keys)
            .Input(arguments)
            .FlagKey(keys), rt => rt.ThrowOrValue());

        public bool ScriptExists(string sha1) => Call("SCRIPT".SubCommand("EXISTS").InputRaw(sha1), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<bool>()));
        public bool[] ScriptExists(string[] sha1) => Call("SCRIPT".SubCommand("EXISTS").Input(sha1), rt => rt.ThrowOrValue<bool[]>());

        public void ScriptFlush() => Call("SCRIPT".SubCommand("FLUSH"), rt => rt.ThrowOrNothing());
        public void ScriptKill() => Call("SCRIPT".SubCommand("KILL"), rt => rt.ThrowOrNothing());
        public string ScriptLoad(string script) => Call("SCRIPT".SubCommand("LOAD").InputRaw(script), rt => rt.ThrowOrValue<string>());
    }
}

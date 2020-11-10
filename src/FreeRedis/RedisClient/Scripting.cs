using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<object> EvalAsync(string script, string[] keys = null, params object[] arguments) => CallAsync("EVAL"
            .Input(script, keys?.Length ?? 0)
            .InputKeyIf(keys?.Any() == true, keys)
            .Input(arguments), rt => rt.ThrowOrValue());

        public Task<object> EvalShaAsync(string sha1, string[] keys = null, params object[] arguments) => CallAsync("EVALSHA"
            .Input(sha1, keys?.Length ?? 0)
            .InputKeyIf(keys?.Any() == true, keys)
            .Input(arguments), rt => rt.ThrowOrValue());

        public Task<bool> ScriptExistsAsync(string sha1) => CallAsync("SCRIPT".SubCommand("EXISTS").InputRaw(sha1), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<bool>()));
        public Task<bool[]> ScriptExistsAsync(string[] sha1) => CallAsync("SCRIPT".SubCommand("EXISTS").Input(sha1), rt => rt.ThrowOrValue<bool[]>());

        public Task ScriptFlushAsync() => CallAsync("SCRIPT".SubCommand("FLUSH"), rt => rt.ThrowOrNothing());
        public Task ScriptKillAsync() => CallAsync("SCRIPT".SubCommand("KILL"), rt => rt.ThrowOrNothing());
        public Task<string> ScriptLoadAsync(string script) => CallAsync("SCRIPT".SubCommand("LOAD").InputRaw(script), rt => rt.ThrowOrValue<string>());
        #endregion
#endif

        public object Eval(string script, string[] keys = null, params object[] arguments) => Call("EVAL"
            .Input(script, keys?.Length ?? 0)
            .InputKeyIf(keys?.Any() == true, keys)
            .Input(arguments), rt => rt.ThrowOrValue());

        public object EvalSha(string sha1, string[] keys = null, params object[] arguments) => Call("EVALSHA"
            .Input(sha1, keys?.Length ?? 0)
            .InputKeyIf(keys?.Any() == true, keys)
            .Input(arguments), rt => rt.ThrowOrValue());

        public bool ScriptExists(string sha1) => Call("SCRIPT".SubCommand("EXISTS").InputRaw(sha1), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<bool>()));
        public bool[] ScriptExists(string[] sha1) => Call("SCRIPT".SubCommand("EXISTS").Input(sha1), rt => rt.ThrowOrValue<bool[]>());

        public void ScriptFlush() => Call("SCRIPT".SubCommand("FLUSH"), rt => rt.ThrowOrNothing());
        public void ScriptKill() => Call("SCRIPT".SubCommand("KILL"), rt => rt.ThrowOrNothing());
        public string ScriptLoad(string script) => Call("SCRIPT".SubCommand("LOAD").InputRaw(script), rt => rt.ThrowOrValue<string>());
    }
}

using FreeRedis.Internal;
using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string[] AclCat(string categoryname = null) => Call("ACL"
            .SubCommand("CAT")
            .InputIf(string.IsNullOrWhiteSpace(categoryname) == false, categoryname), rt => rt.ThrowOrValue<string[]>());

        public long AclDelUser(params string[] username) => Call("ACL".SubCommand("DELUSER").Input(username), rt => rt.ThrowOrValue<long>());
        public string AclGenPass(int bits = 0) => Call("ACL"
            .SubCommand( "GENPASS")
            .InputIf(bits > 0, bits), rt => rt.ThrowOrValue<string>());

        public object AclGetUser(string username = "default") => Call("ACL".SubCommand("GETUSER").InputRaw(username), rt => rt.ThrowOrValue());
        public object AclHelp() => Call("ACL".SubCommand("HELP"), rt => rt.ThrowOrValue());
        public string[] AclList() => Call("ACL".SubCommand("LIST"), rt => rt.ThrowOrValue<string[]>());
        public string AclLoad() => Call("ACL".SubCommand("LOAD"), rt => rt.ThrowOrValue<string>());
        public LogInfo[] AclLog(long count = 0) => Call("ACL"
            .SubCommand("LOG")
            .InputIf(count > 0, count), rt => rt
            .ThrowOrValue((a, _) => a.Select(x => x.ConvertTo<object[]>().MapToClass<LogInfo>(rt.Encoding)).ToArray()));
        public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }

        public string AclSave() => Call("ACL".SubCommand("SAVE"), rt => rt.ThrowOrValue<string>());
        public string AclSetUser(params string[] rule) => Call("ACL".SubCommand("SETUSER").Input(rule), rt => rt.ThrowOrValue<string>());
        public string[] AclUsers() => Call("ACL".SubCommand("USERS"), rt => rt.ThrowOrValue<string[]>());
        public string AclWhoami() => Call("ACL".SubCommand("WHOAMI"), rt => rt.ThrowOrValue<string>());

        public string BgRewriteAof() => Call("BGREWRITEAOF", rt => rt.ThrowOrValue<string>());
        public string BgSave(string schedule = null) => Call("BGSAVE".SubCommand(null).InputIf(string.IsNullOrEmpty(schedule) == false, schedule), rt => rt.ThrowOrValue<string>());
        public object[] Command() => Call("COMMAND", rt => rt.ThrowOrValue<object[]>());
        public long CommandCount() => Call("COMMAND".SubCommand("COUNT"), rt => rt.ThrowOrValue<long>());
        public string[] CommandGetKeys(params string[] command) => Call<string[]>("COMMAND".SubCommand("GETKEYS").Input(command), rt => rt.ThrowOrValue<string[]>());
        public object[] CommandInfo(params string[] command) => Call("COMMAND".SubCommand("INFO").Input(command), rt => rt.ThrowOrValue<object[]>());

        public Dictionary<string, string> ConfigGet(string parameter) => Call("CONFIG".SubCommand("GET").InputRaw(parameter), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public string ConfigResetStat() => Call("CONFIG".SubCommand("RESETSTAT"), rt => rt.ThrowOrValue<string>());
        public string ConfigRewrite() => Call("CONFIG".SubCommand("REWRITE"), rt => rt.ThrowOrValue<string>());
        public string ConfigSet<T>(string parameter, T value) => Call("CONFIG".SubCommand("SET").InputRaw(parameter).InputRaw(value), rt => rt.ThrowOrValue<string>());

        public long DbSize() => Call("DBSIZE", rt => rt.ThrowOrValue<long>());
        public string DebugObject(string key) => Call("DEBUG".SubCommand("OBJECT").InputRaw(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string DebugSegfault() => Call("DEBUG".SubCommand("SEGFAULT"), rt => rt.ThrowOrValue<string>());
        public string FlushAll(bool isasync = false) => Call("FLUSHALL".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue<string>());
        public string FlushDb(bool isasync = false) => Call("FLUSHDB".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue<string>());
        public string Info(string section = null) => Call("INFO".Input(section), rt => rt.ThrowOrValue<string>());

        public long LastSave() => Call("LASTSAVE", rt => rt.ThrowOrValue<long>());
        public string LatencyDoctor() => Call("LATENCY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        public string LatencyGraph(string @event) => Call("LATENCY".SubCommand("GRAPH").InputRaw(@event), rt => rt.ThrowOrValue<string>());
        public string[] LatencyHelp() => Call("LATENCY".SubCommand("HELP"), rt => rt.ThrowOrValue<string[]>());
        public string[][] LatencyHistory(string @event) => Call("LATENCY".SubCommand("HISTORY").InputRaw(@event), rt => rt.ThrowOrValue<string[][]>());
        public string[][] LatencyLatest() => Call("LATENCY".SubCommand("LATEST"), rt => rt.ThrowOrValue<string[][]>());
        public long LatencyReset(string @event) => Call("LATENCY".SubCommand("RESET").InputRaw(@event), rt => rt.ThrowOrValue<long>());

        public string Lolwut(string version) => Call("LOLWUT".SubCommand(null).InputIf(string.IsNullOrWhiteSpace(version) == false, "VERSION", version), rt => rt.ThrowOrValue<string>());
        public string MemoryDoctor() => Call("MEMORY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        public string[] MemoryHelp() => Call("MEMORY".SubCommand("HELP"), rt => rt.ThrowOrValue<string[]>());
        public string MemoryMallocStats() => Call("MEMORY".SubCommand("MALLOC-STATS"), rt => rt.ThrowOrValue<string>());
        public string MemoryPurge() => Call("MEMORY".SubCommand("PURGE"), rt => rt.ThrowOrValue<string>());
        public Dictionary<string, string> MemoryStats() => Call("MEMORY".SubCommand("STATS"), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public long MemoryUsage(string key, long count = 0) => Call("MEMORY"
            .SubCommand( "USAGE")
            .InputRaw(key)
            .InputIf(count > 0, "SAMPLES", count)
            .FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[][] ModuleList() => Call("MODULE".SubCommand("LIST"), rt => rt.ThrowOrValue<string[][]>());
        public string ModuleLoad(string path, params string[] args) => Call("MODULE".SubCommand( "LOAD").InputRaw(path).InputIf(args?.Any() == true, args), rt => rt.ThrowOrValue<string>());
        public string ModuleUnload(string name) => Call("MODULE".SubCommand("UNLOAD").InputRaw(name), rt => rt.ThrowOrValue<string>());

        //public RedisClient Monitor(Action<object> onData)
        //{
        //    IRedisSocket rds = null;
        //    rds = CallReadWhile(onData, () => rds.IsConnected, "MONITOR");
        //    return rds.Client;
        //}
        //public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
        public string ReplicaOf(string host, int port) => Call("REPLICAOF".Input(host, port), rt => rt.ThrowOrValue<string>());
        public RoleResult Role() => Call("ROLE", rt => rt.ThrowOrValueToRole());
        public string Save() => Call("SAVE", rt => rt.ThrowOrValue<string>());

        public string Shutdown(bool save) => Call("SHUTDOWN".Input(save ? "SAVE" : "NOSAVE"), rt => rt.ThrowOrValue<string>());
        public string SlaveOf(string host, int port) => Call("SLAVEOF".Input(host, port), rt => rt.ThrowOrValue<string>());
        public object SlowLog(string subcommand, params string[] argument) => Call("SLOWLOG".SubCommand(subcommand).Input(argument), rt => rt.ThrowOrValue());
        public string SwapDb(int index1, int index2) => Call("SWAPDB".Input(index1, index2), rt => rt.ThrowOrValue<string>());
        //public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
        public DateTime Time() => Call("TIME", rt => rt.ThrowOrValue((a, _) => new DateTime(1970, 0, 0).AddSeconds(a[0].ConvertTo<long>()).AddTicks(a[1].ConvertTo<long>() * 10)));
    }
}

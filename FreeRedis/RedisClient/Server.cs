using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public string[] AclCat(string categoryname = null) => Call<string[]>("ACL"
			.SubCommand("CAT")
			.InputIf(string.IsNullOrWhiteSpace(categoryname) == false, categoryname), rt => rt.ThrowOrValue());
		public long AclDelUser(params string[] username) => username?.Any() == true ? Call<long>("ACL".SubCommand("DELUSER").Input(username), rt => rt.ThrowOrValue()) : throw new ArgumentException(nameof(username));
		public string AclGenPass(int bits = 0) => Call<string>("ACL"
			.SubCommand( "GENPASS")
			.InputIf(bits > 0, bits), rt => rt.ThrowOrValue());
		public object AclGetUser(string username = "default") => Call<object>("ACL".SubCommand("GETUSER").InputRaw(username), rt => rt.ThrowOrValue());
		public object AclHelp() => Call<object>("ACL".SubCommand("HELP"), rt => rt.ThrowOrValue());
		public string[] AclList() => Call<string[]>("ACL".SubCommand("LIST"), rt => rt.ThrowOrValue());
		public string AclLoad() => Call<string>("ACL".SubCommand("LOAD"), rt => rt.ThrowOrValue());
		public LogInfo[] AclLog(long count = 0) => Call<object[][], LogInfo[]>("ACL"
			.SubCommand("LOG")
			.InputIf(count > 0, count), rt => rt
			.NewValue(x => x.Select(a => a.MapToClass<LogInfo>(rt.Encoding)).ToArray()).ThrowOrValue());
		public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }
		public string AclSave() => Call<string>("ACL".SubCommand("SAVE"), rt => rt.ThrowOrValue());
		public string AclSetUser(params string[] rule) => rule?.Any() == true ? Call<string>("ACL".SubCommand("SETUSER").Input(rule), rt => rt.ThrowOrValue()) : throw new ArgumentException(nameof(rule));
		public string[] AclUsers() => Call<string[]>("ACL".SubCommand("USERS"), rt => rt.ThrowOrValue());
		public string AclWhoami() => Call<string>("ACL".SubCommand("WHOAMI"), rt => rt.ThrowOrValue());
		public string BgRewriteAof() => Call<string>("BGREWRITEAOF", rt => rt.ThrowOrValue());
		public string BgSave(string schedule = null) => Call<string>("BGSAVE".SubCommand(null).InputIf(string.IsNullOrEmpty(schedule) == false, schedule), rt => rt.ThrowOrValue());
		public object[] Command() => Call<object[]>("COMMAND", rt => rt.ThrowOrValue());
		public long CommandCount() => Call<long>("COMMAND".SubCommand("COUNT"), rt => rt.ThrowOrValue());
		public string[] CommandGetKeys(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND".SubCommand("GETKEYS").Input(command), rt => rt.ThrowOrValue()) : throw new ArgumentException(nameof(command));
		public string[] CommandInfo(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND".SubCommand("INFO").Input( command), rt => rt.ThrowOrValue()) : throw new ArgumentException(nameof(command));
		public Dictionary<string, string> ConfigGet(string parameter) => Call<string[], Dictionary<string, string>>("CONFIG".SubCommand("GET").InputRaw(parameter), rt => rt.NewValue(a => a.MapToHash<string>(rt.Encoding)).ThrowOrValue());
		public string ConfigResetStat() => Call<string>("CONFIG".SubCommand("RESETSTAT"), rt => rt.ThrowOrValue());
		public string ConfigRewrite() => Call<string>("CONFIG".SubCommand("REWRITE"), rt => rt.ThrowOrValue());
		public string ConfigSet(string parameter, object value) => Call<string>("CONFIG".SubCommand("SET").InputRaw(parameter).InputRaw(value), rt => rt.ThrowOrValue());
		public long DbSize() => Call<long>("DBSIZE", rt => rt.ThrowOrValue());
		public string DebugObject(string key) => Call<string>("DEBUG".SubCommand("OBJECT").InputRaw(key).FlagKey(key), rt => rt.ThrowOrValue());
		public string DebugSegfault() => Call<string>("DEBUG".SubCommand("SEGFAULT"), rt => rt.ThrowOrValue());
		public string FlushAll(bool isasync = false) => Call<string>("FLUSHALL".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue());
		public string FlushDb(bool isasync = false) => Call<string>("FLUSHDB".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue());
		public string Info(string section = null) => Call<string>("INFO".Input(section), rt => rt.ThrowOrValue());
		public long LastSave() => Call<long>("LASTSAVE", rt => rt.ThrowOrValue());
		public string LatencyDoctor() => Call<string>("LATENCY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue());
		public string LatencyGraph(string @event) => Call<string>("LATENCY".SubCommand("GRAPH").InputRaw(@event), rt => rt.ThrowOrValue());
		public string[] LatencyHelp() => Call<string[]>("LATENCY".SubCommand("HELP"), rt => rt.ThrowOrValue());
		public string[][] LatencyHistory(string @event) => Call<string[][]>("HISTORY".SubCommand("HELP").InputRaw(@event), rt => rt.ThrowOrValue());
		public string[][] LatencyLatest() => Call<string[][]>("HISTORY".SubCommand("LATEST"), rt => rt.ThrowOrValue());
		public long LatencyReset(string @event) => Call<long>("LASTSAVE".SubCommand("RESET").InputRaw(@event), rt => rt.ThrowOrValue());
		public string Lolwut(string version) => Call<string>("LATENCY".SubCommand(null).InputIf(string.IsNullOrWhiteSpace(version) == false, "VERSION", version), rt => rt.ThrowOrValue());
		public string MemoryDoctor() => Call<string>("MEMORY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue());
		public string[] MemoryHelp() => Call<string[]>("MEMORY".SubCommand("HELP"), rt => rt.ThrowOrValue());
		public string MemoryMallocStats() => Call<string>("MEMORY".SubCommand("MALLOC-STATS"), rt => rt.ThrowOrValue());
		public string MemoryPurge() => Call<string>("MEMORY".SubCommand("PURGE"), rt => rt.ThrowOrValue());
		public Dictionary<string, string> MemoryStats() => Call<string[], Dictionary<string, string>>("MEMORY".SubCommand("STATS"), rt => rt.NewValue(a => a.MapToHash<string>(rt.Encoding)).ThrowOrValue());
		public long MemoryUsage(string key, long count = 0) => Call<long>("MEMORY"
			.SubCommand( "USAGE")
			.InputRaw(key)
			.InputIf(count > 0, "SAMPLES", count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[][] ModuleList() => Call<string[][]>("MODULE".SubCommand("LIST"), rt => rt.ThrowOrValue());
		public string ModuleLoad(string path, params string[] args) => Call<string>("MODULE".SubCommand( "LOAD").InputIf(args?.Any() == true, args), rt => rt.ThrowOrValue());
		public string ModuleUnload(string name) => Call<string>("MODULE".SubCommand("UNLOAD").InputRaw(name), rt => rt.ThrowOrValue());
		public RedisClient Monitor(Action<object> onData)
		{
			IRedisSocket rds = null;
			rds = CallReadWhile(onData, () => rds.IsConnected, "MONITOR");
			return rds.Client;
		}
		//public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
		public string ReplicaOf(string host, int port) => Call<string>("REPLICAOF".Input(host, port), rt => rt.ThrowOrValue());
		public object Role() => Call<object>("ROLE", rt => rt.ThrowOrValue());
		public string Save() => Call<string>("SAVE", rt => rt.ThrowOrValue());
		public string Shutdown(bool save) => Call<string>("SHUTDOWN".Input(save ? "SAVE" : "NOSAVE"), rt => rt.ThrowOrValue());
		public string SlaveOf(string host, int port) => Call<string>("SLAVEOF".Input(host, port), rt => rt.ThrowOrValue());
		public object SlowLog(string subcommand, params string[] argument) => Call<object>("SLOWLOG".SubCommand(subcommand).Input(argument), rt => rt.ThrowOrValue());
		public string SwapDb(int index1, int index2) => Call<string>("SWAPDB".Input(index1, index2), rt => rt.ThrowOrValue());
		//public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
		public DateTime Time() => Call<long[], DateTime>("TIME", rt => rt.NewValue(a => new DateTime(1970, 0, 0).AddSeconds(a[0]).AddTicks(a[1] * 10)).ThrowOrValue());
    }
}

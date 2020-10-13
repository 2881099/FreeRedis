using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string[]> AclCat(string categoryname = null) => Call<string[]>("ACL"
			.SubCommand("CAT")
			.InputIf(string.IsNullOrWhiteSpace(categoryname) == false, categoryname));
		public RedisResult<long> AclDelUser(params string[] username) => username?.Any() == true ? Call<long>("ACL".SubCommand("DELUSER").Input(username)) : throw new ArgumentException(nameof(username));
		public RedisResult<string> AclGenPass(int bits = 0) => Call<string>("ACL"
			.SubCommand( "GENPASS")
			.InputIf(bits > 0, bits));
		public RedisResult<object> AclGetUser(string username = "default") => Call<object>("ACL".SubCommand("GETUSER").InputRaw(username));
		public RedisResult<object> AclHelp() => Call<object>("ACL".SubCommand("HELP"));
		public RedisResult<string[]> AclList() => Call<string[]>("ACL".SubCommand("LIST"));
		public RedisResult<string> AclLoad() => Call<string>("ACL".SubCommand("LOAD"));
		public RedisResult<LogInfo[]> AclLog(long count = 0) => Call<object[][]>("ACL"
			.SubCommand("LOG")
			.InputIf(count > 0, count))
			.NewValue(x => x.Select(a => a.MapToClass<LogInfo>(Encoding)).ToArray());
		public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }
		public RedisResult<string> AclSave() => Call<string>("ACL".SubCommand("SAVE"));
		public RedisResult<string> AclSetUser(params string[] rule) => rule?.Any() == true ? Call<string>("ACL".SubCommand("SETUSER").Input(rule)) : throw new ArgumentException(nameof(rule));
		public RedisResult<string[]> AclUsers() => Call<string[]>("ACL".SubCommand("USERS"));
		public RedisResult<string> AclWhoami() => Call<string>("ACL".SubCommand("WHOAMI"));
		public RedisResult<string> BgRewriteAof() => Call<string>("BGREWRITEAOF");
		public RedisResult<string> BgSave(string schedule = null) => Call<string>("BGSAVE".SubCommand(null).InputIf(string.IsNullOrEmpty(schedule) == false, schedule));
		public RedisResult<object[]> Command() => Call<object[]>("COMMAND");
		public RedisResult<long> CommandCount() => Call<long>("COMMAND".SubCommand("COUNT"));
		public RedisResult<string[]> CommandGetKeys(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND".SubCommand("GETKEYS").Input(command)) : throw new ArgumentException(nameof(command));
		public RedisResult<string[]> CommandInfo(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND".SubCommand("INFO").Input( command)) : throw new ArgumentException(nameof(command));
		public RedisResult<Dictionary<string, string>> ConfigGet(string parameter) => Call<string[]>("CONFIG".SubCommand("GET").InputRaw(parameter)).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<string> ConfigResetStat() => Call<string>("CONFIG".SubCommand("RESETSTAT"));
		public RedisResult<string> ConfigRewrite() => Call<string>("CONFIG".SubCommand("REWRITE"));
		public RedisResult<string> ConfigSet(string parameter, object value) => Call<string>("CONFIG".SubCommand("SET").InputRaw(parameter).InputRaw(value));
		public RedisResult<long> DbSize() => Call<long>("DBSIZE");
		public RedisResult<string> DebugObject(string key) => Call<string>("DEBUG".SubCommand("OBJECT").InputRaw(key).FlagKey(key));
		public RedisResult<string> DebugSegfault() => Call<string>("DEBUG".SubCommand("SEGFAULT"));
		public RedisResult<string> FlushAll(bool isasync = false) => Call<string>("FLUSHALL".SubCommand(null).InputIf(isasync, "ASYNC"));
		public RedisResult<string> FlushDb(bool isasync = false) => Call<string>("FLUSHDB".SubCommand(null).InputIf(isasync, "ASYNC"));
		public RedisResult<string> Info(string section = null) => Call<string>("INFO".Input(section));
		public RedisResult<long> LastSave() => Call<long>("LASTSAVE");
		public RedisResult<string> LatencyDoctor() => Call<string>("LATENCY".SubCommand("DOCTOR"));
		public RedisResult<string> LatencyGraph(string @event) => Call<string>("LATENCY".SubCommand("GRAPH").InputRaw(@event));
		public RedisResult<string[]> LatencyHelp() => Call<string[]>("LATENCY".SubCommand("HELP"));
		public RedisResult<string[][]> LatencyHistory(string @event) => Call<string[][]>("HISTORY".SubCommand("HELP").InputRaw(@event));
		public RedisResult<string[][]> LatencyLatest() => Call<string[][]>("HISTORY".SubCommand("LATEST"));
		public RedisResult<long> LatencyReset(string @event) => Call<long>("LASTSAVE".SubCommand("RESET").InputRaw(@event));
		public RedisResult<string> Lolwut(string version) => Call<string>("LATENCY".SubCommand(null).InputIf(string.IsNullOrWhiteSpace(version) == false, "VERSION", version));
		public RedisResult<string> MemoryDoctor() => Call<string>("MEMORY".SubCommand("DOCTOR"));
		public RedisResult<string[]> MemoryHelp() => Call<string[]>("MEMORY".SubCommand("HELP"));
		public RedisResult<string> MemoryMallocStats() => Call<string>("MEMORY".SubCommand("MALLOC-STATS"));
		public RedisResult<string> MemoryPurge() => Call<string>("MEMORY".SubCommand("PURGE"));
		public RedisResult<Dictionary<string, string>> MemoryStats() => Call<string[]>("MEMORY".SubCommand("STATS")).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<long> MemoryUsage(string key, long count = 0) => Call<long>("MEMORY"
			.SubCommand( "USAGE")
			.InputRaw(key)
			.InputIf(count > 0, "SAMPLES", count)
			.FlagKey(key));
		public RedisResult<string[][]> ModuleList() => Call<string[][]>("MODULE".SubCommand("LIST"));
		public RedisResult<string> ModuleLoad(string path, params string[] args) => Call<string>("MODULE".SubCommand( "LOAD").InputIf(args?.Any() == true, args));
		public RedisResult<string> ModuleUnload(string name) => Call<string>("MODULE".SubCommand("UNLOAD").InputRaw(name));
		public void Monitor(Action<object> onData) => CallReadWhile(onData, () => IsConnected, "MONITOR");
		//public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
		public RedisResult<string> ReplicaOf(string host, int port) => Call<string>("REPLICAOF".Input(host, port));
		public RedisResult<object> Role() => Call<object>("ROLE");
		public RedisResult<string> Save() => Call<string>("SAVE");
		public RedisResult<string> Shutdown(bool save) => Call<string>("SHUTDOWN".Input(save ? "SAVE" : "NOSAVE"));
		public RedisResult<string> SlaveOf(string host, int port) => Call<string>("SLAVEOF".Input(host, port));
		public RedisResult<object> SlowLog(string subcommand, params string[] argument) => Call<object>("SLOWLOG".SubCommand(subcommand).Input(argument));
		public RedisResult<string> SwapDb(int index1, int index2) => Call<string>("SWAPDB".Input(index1, index2));
		//public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
		public RedisResult<DateTime> Time() => Call<long[]>("TIME").NewValue(a => new DateTime(1970, 0, 0).AddSeconds(a[0]).AddTicks(a[1] * 10));
    }
}

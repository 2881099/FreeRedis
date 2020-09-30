using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string[]> AclCat(string categoryname = null) => string.IsNullOrWhiteSpace(categoryname) ? Call<string[]>("ACL", "CAT") : Call<string[]>("ACL", "CAT", categoryname);
		public RedisResult<long> AclDelUser(params string[] username) => username?.Any() == true ? Call<long>("ACL", "DELUSER", username) : throw new ArgumentException(nameof(username));
		public RedisResult<string> AclGenPass(int bits = 0) => bits <= 0 ? Call<string>("ACL", "GENPASS") : Call<string>("ACL", "GENPASS", bits);
		public RedisResult<object> AclGetUser(string username = "default") => Call<object>("ACL", "GETUSER", username);
		public RedisResult<object> AclHelp() => Call<object>("ACL", "HELP");
		public RedisResult<string[]> AclList() => Call<string[]>("ACL", "LIST");
		public RedisResult<string> AclLoad() => Call<string>("ACL", "LOAD");
		public RedisResult<LogInfo[]> AclLog(long count = 0) => (count <= 0 ? Call<object[][]>("ACL", "LOG") : Call<object[][]>("ACL", "LOG", count)).NewValue(x => x.Select(a => a.MapToClass<LogInfo>(Encoding)).ToArray());
		public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }
		public RedisResult<string> AclSave() => Call<string>("ACL", "SAVE");
		public RedisResult<string> AclSetUser(params string[] rule) => rule?.Any() == true ? Call<string>("ACL", "SETUSER", rule) : throw new ArgumentException(nameof(rule));
		public RedisResult<string[]> AclUsers() => Call<string[]>("ACL", "USERS");
		public RedisResult<string> AclWhoami() => Call<string>("ACL", "WHOAMI");
		public RedisResult<string> BgRewriteAof() => Call<string>("BGREWRITEAOF");
		public RedisResult<string> BgSave(string schedule = null) => Call<string>("BGSAVE", schedule);
		public RedisResult<object[]> Command() => Call<object[]>("COMMAND");
		public RedisResult<long> CommandCount() => Call<long>("COMMAND", "COUNT");
		public RedisResult<string[]> CommandGetKeys(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND", "GETKEYS", command) : throw new ArgumentException(nameof(command));
		public RedisResult<string[]> CommandInfo(params string[] command) => command?.Any() == true ? Call<string[]>("COMMAND", "INFO", command) : throw new ArgumentException(nameof(command));
		public RedisResult<Dictionary<string, string>> ConfigGet(string parameter) => Call<string[]>("CONFIG", "GET", parameter).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<string> ConfigResetStat() => Call<string>("CONFIG", "RESETSTAT");
		public RedisResult<string> ConfigRewrite() => Call<string>("CONFIG", "REWRITE");
		public RedisResult<string> ConfigSet(string parameter, object value) => Call<string>("CONFIG", "SET", parameter, value);
		public RedisResult<long> DbSize() => Call<long>("DBSIZE");
		public RedisResult<string> DebugObject(string key) => Call<string>("DEBUG", "OBJECT", key);
		public RedisResult<string> DebugSegfault() => Call<string>("DEBUG", "SEGFAULT");
		public RedisResult<string> FlushAll(bool isasync = false) => Call<string>("FLUSHALL", isasync ? "ASYNC" : null);
		public RedisResult<string> FlushDb(bool isasync = false) => Call<string>("FLUSHDB", isasync ? "ASYNC" : null);
		public RedisResult<string> Info(string section = null) => Call<string>("INFO", section);
		public RedisResult<long> LastSave() => Call<long>("LASTSAVE");
		public RedisResult<string> LatencyDoctor() => Call<string>("LATENCY", "DOCTOR");
		public RedisResult<string> LatencyGraph(string @event) => Call<string>("LATENCY", "GRAPH", @event);
		public RedisResult<string[]> LatencyHelp() => Call<string[]>("LATENCY", "HELP");
		public RedisResult<string[][]> LatencyHistory(string @event) => Call<string[][]>("HISTORY", "HELP", @event);
		public RedisResult<string[][]> LatencyLatest() => Call<string[][]>("HISTORY", "LATEST");
		public RedisResult<long> LatencyReset(string @event) => Call<long>("LASTSAVE", "RESET", @event);
		public RedisResult<string> Lolwut(string version) => Call<string>("LATENCY", string.IsNullOrWhiteSpace(version) ? null : $"VERSION {version}");
		public RedisResult<string> MemoryDoctor() => Call<string>("MEMORY", "DOCTOR");
		public RedisResult<string[]> MemoryHelp() => Call<string[]>("MEMORY", "HELP");
		public RedisResult<string> MemoryMallocStats() => Call<string>("MEMORY", "MALLOC-STATS");
		public RedisResult<string> MemoryPurge() => Call<string>("MEMORY", "PURGE");
		public RedisResult<Dictionary<string, string>> MemoryStats() => Call<string[]>("MEMORY", "STATS").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<long> MemoryUsage(string key, long count = 0) => count <= 0 ? Call<long>("MEMORY ", "USAGE", key) : Call<long>("MEMORY ", "USAGE", key, "SAMPLES", count);
		public RedisResult<string[][]> ModuleList() => Call<string[][]>("MODULE", "LIST");
		public RedisResult<string> ModuleLoad(string path, params string[] args) => Call<string>("MODULE", "LOAD", path.AddIf(args?.Any() == true, args).ToArray());
		public RedisResult<string> ModuleUnload(string name) => Call<string>("MODULE", "UNLOAD", name);
		public void Monitor(Action<object> onData) => CallReadWhile(onData, () => IsConnected, "MONITOR");
		//public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
		public RedisResult<string> ReplicaOf(string host, int port) => Call<string>("REPLICAOF", host, port);
		public RedisResult<object> Role() => Call<object>("ROLE");
		public RedisResult<string> Save() => Call<string>("SAVE");
		public RedisResult<string> Shutdown(bool save) => Call<string>("SHUTDOWN", save ? "SAVE" : "NOSAVE");
		public RedisResult<string> SlaveOf(string host, int port) => Call<string>("SLAVEOF", host, port);
		public RedisResult<object> SlowLog(string subcommand, params string[] argument) => Call<object>("SLOWLOG", subcommand, argument);
		public RedisResult<string> SwapDb(int index1, int index2) => Call<string>("SWAPDB", null, index1, index2);
		//public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
		public RedisResult<DateTime> Time() => Call<long[]>("TIME").NewValue(a => new DateTime(1970, 0, 0).AddSeconds(a[0]).AddTicks(a[1] * 10));
    }
}

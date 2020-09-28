using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public RedisResult<T> SendCommand<T>(string cmd, string subcmd = null, params object[] parms)
        {
            var args = PrepareCommand(cmd, subcmd, parms);
            Resp3Helper.Write(Stream, Encoding, args, true);
            var result = Resp3Helper.Read<T>(Stream, Encoding);
            return result;
		}
		public void SendCommandListen(Action<string> ondata, Func<bool> next, string command, string subcommand = null, params object[] parms)
		{
			var args = PrepareCommand(command, subcommand, parms);
			Resp3Helper.Write(Stream, args, true);
			_listeningCommand = string.Join(" ", args);
			do
			{
				try
				{
					var data = Resp3Helper.Read<string>(Stream).Value;
					ondata?.Invoke(data);
				}
				catch (IOException ex)
				{
					Console.WriteLine(ex.Message);
					if (IsConnected) throw;
					break;
				}
			} while (next());
			_listeningCommand = null;
		}

		public RedisResult<string[]> AclCat(string categoryname = null) => string.IsNullOrWhiteSpace(categoryname) ? SendCommand<string[]>("ACL", "CAT") : SendCommand<string[]>("ACL", "CAT", categoryname);
		public RedisResult<int> AclDelUser(params string[] username) => username?.Any() == true ? SendCommand<int>("ACL", "DELUSER", username) : throw new ArgumentException(nameof(username));
		public RedisResult<string> AclGenPass(int bits = 0) => bits <= 0 ? SendCommand<string>("ACL", "GENPASS") : SendCommand<string>("ACL", "GENPASS", bits);
		public RedisResult<string[]> AclList() => SendCommand<string[]>("ACL", "LIST");
		public RedisResult<string> AclLoad() => SendCommand<string>("ACL", "LOAD");
		public RedisResult<LogInfo[]> AclLog(long count = 0) => (count <= 0 ? SendCommand<object[][]>("ACL", "LOG") : SendCommand<object[][]>("ACL", "LOG", count)).NewValue(x => x.Select(a => a.MapToClass<LogInfo>(Encoding)).ToArray());
		public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }
		public RedisResult<string> AclSave() => SendCommand<string>("ACL", "SAVE");
		public RedisResult<string> AclSetUser(params string[] rule) => rule?.Any() == true ? SendCommand<string>("ACL", "SETUSER", rule) : throw new ArgumentException(nameof(rule));
		public RedisResult<string[]> AclUsers() => SendCommand<string[]>("ACL", "USERS");
		public RedisResult<string> AclWhoami() => SendCommand<string>("ACL", "WHOAMI");
		public RedisResult<string> BgRewriteAof() => SendCommand<string>("BGREWRITEAOF");
		public RedisResult<string> BgSave(string schedule = null) => SendCommand<string>("BGSAVE", schedule);
		public RedisResult<object[]> Command() => SendCommand<object[]>("COMMAND");
		public RedisResult<int> CommandCount() => SendCommand<int>("COMMAND", "COUNT");
		public RedisResult<string[]> CommandGetKeys(params string[] command) => command?.Any() == true ? SendCommand<string[]>("COMMAND", "GETKEYS", command) : throw new ArgumentException(nameof(command));
		public RedisResult<string[]> CommandInfo(params string[] command) => command?.Any() == true ? SendCommand<string[]>("COMMAND", "INFO", command) : throw new ArgumentException(nameof(command));
		public RedisResult<Dictionary<string, string>> ConfigGet(string parameter) => SendCommand<string[]>("CONFIG", "GET", parameter).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<string> ConfigResetStat() => SendCommand<string>("CONFIG", "RESETSTAT");
		public RedisResult<string> ConfigRewrite() => SendCommand<string>("CONFIG", "REWRITE");
		public RedisResult<string> ConfigSet(string parameter, object value) => SendCommand<string>("CONFIG", "SET", parameter, value);
		public RedisResult<long> DbSize() => SendCommand<long>("DBSIZE");
		public RedisResult<string> DebugObject(string key) => SendCommand<string>("DEBUG", "OBJECT", key);
		public RedisResult<string> DebugSegfault() => SendCommand<string>("DEBUG", "SEGFAULT");
		public RedisResult<string> FlushAll(bool isasync = false) => SendCommand<string>("FLUSHALL", isasync ? "ASYNC" : null);
		public RedisResult<string> FlushDb(bool isasync = false) => SendCommand<string>("FLUSHDB", isasync ? "ASYNC" : null);
		public RedisResult<string> Info(string section = null) => SendCommand<string>("INFO", section);
		public RedisResult<long> LastSave() => SendCommand<long>("LASTSAVE");
		public RedisResult<string> LatencyDoctor() => SendCommand<string>("LATENCY", "DOCTOR");
		public RedisResult<string> LatencyGraph(string @event) => SendCommand<string>("LATENCY", "GRAPH", @event);
		public RedisResult<string[]> LatencyHelp() => SendCommand<string[]>("LATENCY", "HELP");
		public RedisResult<string[][]> LatencyHistory(string @event) => SendCommand<string[][]>("HISTORY", "HELP", @event);
		public RedisResult<string[][]> LatencyLatest() => SendCommand<string[][]>("HISTORY", "LATEST");
		public RedisResult<long> LatencyReset(string @event) => SendCommand<long>("LASTSAVE", "RESET", @event);
		public RedisResult<string> Lolwut(string version) => SendCommand<string>("LATENCY", string.IsNullOrWhiteSpace(version) ? null : $"VERSION {version}");
		public RedisResult<string> MemoryDoctor() => SendCommand<string>("MEMORY", "DOCTOR");
		public RedisResult<string[]> MemoryHelp() => SendCommand<string[]>("MEMORY", "HELP");
		public RedisResult<string> MemoryMallocStats() => SendCommand<string>("MEMORY", "MALLOC-STATS");
		public RedisResult<string> MemoryPurge() => SendCommand<string>("MEMORY", "PURGE");
		public RedisResult<Dictionary<string, string>> MemoryStats() => SendCommand<string[]>("MEMORY", "STATS").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<long> MemoryUsage(string key, long count = 0) => count <= 0 ? SendCommand<long>("MEMORY ", "USAGE", key) : SendCommand<long>("MEMORY ", "USAGE", key, "SAMPLES", count);
		public RedisResult<string[][]> ModuleList() => SendCommand<string[][]>("MODULE", "LIST");
		public RedisResult<string> ModuleLoad(string path, params string[] args) => args?.Any() == true ? SendCommand<string>("MODULE", "LOAD", new[] { path }.Concat(args)) : SendCommand<string>("MODULE", "LOAD", path);
		public RedisResult<string> ModuleUnload(string name) => SendCommand<string>("MODULE", "UNLOAD", name);
		public void Monitor(Action<string> onData) => SendCommandListen(onData, () => IsConnected, "MONITOR");
		//public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
		public RedisResult<string> ReplicaOf(string host, int port) => SendCommand<string>("REPLICAOF", host, port);
		public RedisResult<object> Role() => SendCommand<object>("ROLE");
		public RedisResult<string> Save() => SendCommand<string>("SAVE");
		public RedisResult<string> Shutdown(bool save) => SendCommand<string>("SHUTDOWN", save ? "SAVE" : "NOSAVE");
		public RedisResult<string> SlaveOf(string host, int port) => SendCommand<string>("SLAVEOF", host, port);
		public RedisResult<object> SlowLog(string subcommand, params string[] argument) => SendCommand<object>("SLOWLOG", subcommand, argument);
		public RedisResult<string> SwapDb(int index1, int index2) => SendCommand<string>("SWAPDB", null, index1, index2);
		//public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
		public RedisResult<DateTime> Time() => SendCommand<long[]>("TIME").NewValue(a => new DateTime(1970, 0, 0).AddSeconds(a[0]).AddTicks(a[1] * 10));

		public RedisResult<string> Discard() => SendCommand<string>("DISCARD");
		public RedisResult<object[]> Exec() => SendCommand<object[]>("EXEC");
		public RedisResult<string> Multi() => SendCommand<string>("MULTI");
		public RedisResult<string> UnWatch() => SendCommand<string>("UNWATCH");
		public RedisResult<string> Watch(params string[] keys) => SendCommand<string>("WATCH", null, keys);

		public RedisResult<long> Append(string key, object value) => SendCommand<long>("APPEND", key, value);
		public RedisResult<long> BitCount(string key, long start, long end) => SendCommand<long>("BITCOUNT", key, start, end);
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public RedisResult<long> BitOp(BitOpOperation operation, string destkey, params string[] keys) => SendCommand<long>("BITOP", null, new object[] { operation, destkey }.Concat(keys).ToArray());
		public RedisResult<long> BitPos(string key, object bit, long start = 0, long end = 0) => start > 0 && end > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start, end }) :
			(start > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start }) : SendCommand<long>("BITPOS", key, bit));
		public RedisResult<long> Decr(string key) => SendCommand<long>("DECR", key);
		public RedisResult<long> DecrBy(string key, long decrement) => SendCommand<long>("DECRBY", key, decrement);
		public RedisResult<string> Get(string key) => SendCommand<string>("GET", key);
		public RedisResult<T> Get<T>(string key) => SendCommand<T>("GET", key);
		public RedisResult<long> GetBit(string key, long offset) => SendCommand<long>("GETBIT", key, offset);
		public RedisResult<string> GetRange(string key, long start, long end) => SendCommand<string>("GETRANGE", key, start, end);
		public RedisResult<T> GetRange<T>(string key, long start, long end) => SendCommand<T>("GETRANGE", key, start, end);
		public RedisResult<string> GetSet(string key, object value) => SendCommand<string>("GETSET", key, value);
		public RedisResult<T> GetSet<T>(string key, object value) => SendCommand<T>("GETSET", key, value);
		public RedisResult<long> Incr(string key) => SendCommand<long>("INCR", key);
		public RedisResult<long> IncrBy(string key, long decrement) => SendCommand<long>("INCRBY", key, decrement);
		public RedisResult<decimal> IncrByFloat(string key, decimal decrement) => SendCommand<decimal>("INCRBYFLOAT", key, decrement);
		public RedisResult<string[]> MGet(params string[] keys) => SendCommand<string[]>("MGET", null, keys);
		public RedisResult<T[]> MGet<T>(params string[] keys) => SendCommand<T[]>("MGET", null, keys);
		public RedisResult<string> MSet(Dictionary<string, object> keyValues) => SendCommand<string>("MSET", null, keyValues.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
		public RedisResult<long> MSetNx(Dictionary<string, object> keyValues) => SendCommand<long>("MSETNX", null, keyValues.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
		public RedisResult<string> PSetNx(string key, long milliseconds, object value) => SendCommand<string>("PSETEX", key, milliseconds, value);
		public RedisResult<string> Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx)
		{
			var args = new List<object>();
			args.Add(value);
			if (timeout.TotalSeconds >= 1) args.AddRange(new object[] { "EX", (long)timeout.TotalSeconds });
			else if (timeout.TotalMilliseconds >= 1) args.AddRange(new object[] { "PX", (long)timeout.TotalMilliseconds });
			else if (keepTtl) args.Add("KEEPTTL");
			if (nx) args.Add("NX");
			else if (xx) args.Add("XX");
			return SendCommand<string>("SET", key, value, args);
		}
		public RedisResult<long> SetBit(string key, long offset, object value) => SendCommand<long>("SETBIT", key, offset, value);
		public RedisResult<string> SetEx(string key, int seconds, object value) => SendCommand<string>("SETEX", key, seconds, value);
		public RedisResult<bool> SetNx(string key, object value) => SendCommand<bool>("SETNX", key, value);
		public RedisResult<long> SetRange(string key, long offset, object value) => SendCommand<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => SendCommand<long>("STRLEN", key);


	}
	public enum BitOpOperation { And, Or, Xor, Not }
}

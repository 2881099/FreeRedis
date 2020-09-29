using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	public partial class RedisClient : RedisClientBase, IDisposable
	{
		public RedisClient(string host, bool ssl) : base(host, ssl) { }

		public void Dispose()
        {
			base.SafeReleaseSocket();
        }

		#region Commands Cluster
		public RedisResult<string> ClusterAddSlots(params int[] slot) => Call<string>("CLUSTER", "ADDSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterBumpEpoch() => Call<string>("CLUSTER", "BUMPEPOCH");
		public RedisResult<long> ClusterCountFailureReports(string nodeid) => Call<long>("CLUSTER", "COUNT-FAILURE-REPORTS", nodeid);
		public RedisResult<long> ClusterCountKeysInSlot(int slot) => Call<long>("CLUSTER", "COUNTKEYSINSLOT", slot);
		public RedisResult<string> ClusterDelSlots(params int[] slot) => Call<string>("CLUSTER", "DELSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterFailOver(ClusterFailOverType type) => Call<string>("CLUSTER", "FAILOVER", type);
		public RedisResult<string> ClusterFlushSlots() => Call<string>("CLUSTER", "FLUSHSLOTS");
		public RedisResult<long> ClusterForget(string nodeid) => Call<long>("CLUSTER", "FORGET", nodeid);
		public RedisResult<string[]> ClusterGetKeysInSlot(int slot) => Call<string[]>("CLUSTER", "GETKEYSINSLOT", slot);
		public RedisResult<Dictionary<string, string>> ClusterInfo() => Call<string[]>("CLUSTER", "INFO").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<int> ClusterKeySlot(string key) => Call<int>("CLUSTER", "KEYSLOT", key);
		public RedisResult<string> ClusterMeet(string ip, int port) => Call<string>("CLUSTER", "MEET", ip, port);
		public RedisResult<string> ClusterMyId() => Call<string>("CLUSTER", "MYID");
		public RedisResult<string> ClusterNodes() => Call<string>("CLUSTER", "NODES");
		public RedisResult<string> ClusterReplicas(string nodeid) => Call<string>("CLUSTER", "REPLICAS", nodeid);
		public RedisResult<string> ClusterReplicate(string nodeid) => Call<string>("CLUSTER", "REPLICATE", nodeid);
		public RedisResult<string> ClusterReset(ClusterResetType type) => Call<string>("CLUSTER", "RESET", type);
		public RedisResult<string> ClusterSaveConfig() => Call<string>("CLUSTER", "SAVECONFIG");
		public RedisResult<string> ClusterSetConfigEpoch(string epoch) => Call<string>("CLUSTER", "SET-CONFIG-EPOCH", epoch);
		public RedisResult<string[]> ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => Call<string[]>("CLUSTER", "SETSLOT", ""
			.AddIf(true, slot, type)
			.AddIf(!string.IsNullOrWhiteSpace(nodeid), nodeid)
			.ToArray());
		public RedisResult<string> ClusterSlaves(string nodeid) => Call<string>("CLUSTER", "SLAVES", nodeid);
		public RedisResult<object> ClusterSlots() => Call<object>("CLUSTER", "SLOTS");
		public RedisResult<string> ReadOnly() => Call<string>("READONLY");
		public RedisResult<string> ReadWrite() => Call<string>("READWRITE");
		#endregion

		#region Commands Connection
		public RedisResult<string> Auth(string password) => Call<string>("AUTH", null, password);
		public RedisResult<string> Auth(string username, string password) => Call<string>("AUTH", null, ""
			.AddIf(!string.IsNullOrWhiteSpace(username), username)
			.AddIf(true, password)
			.ToArray());
		public RedisResult<string> ClientCaching(Confirm confirm) => Call<string>("CLIENT", "CACHING", confirm);
		public RedisResult<string> ClientGetName() => Call<string>("CLIENT", "GETNAME");
		public RedisResult<long> ClientGetRedir() => Call<long>("CLIENT", "GETREDIR");
		public RedisResult<long> ClientId() => Call<long>("CLIENT", "ID");
		public RedisResult<long> ClientKill(string ipport, long? clientid) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.ToArray());
		public RedisResult<long> ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.AddIf(type != null, "TYPE", type)
			.AddIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.AddIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.AddIf(skipme != null, "SKIPME", skipme)
			.ToArray());
		public RedisResult<string[]> ClientList(ClientType? type) => Call<string[]>("CLIENT", "LIST", ""
			.AddIf(type != null, "TYPE", type)
			.ToArray());
		public RedisResult<string> ClientPaush(long timeoutMilliseconds) => Call<string>("CLIENT", "PAUSE", timeoutMilliseconds);
		public RedisResult<string> ClientReply(ClientReplyType type) => Call<string>("CLIENT", "REPLY", type);
		public RedisResult<string> ClientSetName(string connectionName) => Call<string>("CLIENT", "SETNAME", connectionName);
		public RedisResult<string> ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call<string>("CLIENT", "TRACKING", ""
			.AddIf(on_off, "ON")
			.AddIf(!on_off, "OFF")
			.AddIf(redirect != null, "REDIRECT", redirect)
			.AddIf(prefix?.Any() == true, prefix.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.AddIf(bcast, "BCAST")
			.AddIf(optin, "OPTIN")
			.AddIf(optout, "OPTOUT")
			.AddIf(noloop, "NOLOOP")
			.ToArray());
		public RedisResult<bool> ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT", "UNBLOCK", ""
			.AddIf(true, clientid)
			.AddIf(type != null, type)
			.ToArray());
		public RedisResult<string> Echo(string message) => Call<string>("ECHO", null, message);
		public RedisResult<object> Hello(decimal protover, string username, string password, string clientname) => Call<object>("HELLO", null, ""
			.AddIf(true, protover)
			.AddIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.AddIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname)
			.ToArray());
		public RedisResult<string> Ping(string message = null) => Call<string>("PING", null, message);
		public RedisResult<string> Quit() => Call<string>("QUIT");
		public RedisResult<string> Select(int index) => Call<string>("SELECT", null, index);
		#endregion

		#region Commands Geo
		public RedisResult<long> GetAdd(string key, params GeoMember[] members) => Call<long>("GEOADD", key, ""
			.AddIf(members?.Any() == true, members.Select(a => new object[] { a.Longitude, a.Latitude, a.Member }).ToArray())
			.ToArray());

		public RedisResult<decimal> GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.M) => Call<decimal>("GEOADD", key, ""
			.AddIf(true, member1, member2)
			.AddIf(unit != GeoUnit.M, unit)
			.ToArray());
		public RedisResult<string[]> GeoHash(string key, string[] members) => Call<string[]>("GEOADD", key, "".AddIf(members?.Any() == true, members).ToArray());
		public RedisResult<GeoMember[]> GeoPos(string key, string[] members) => Call<object>("GEOPOS", key, "".AddIf(members?.Any() == true, members).ToArray())
			.NewValue(a => (a as object[]).Select((z, y) =>
				{
					var zarr = z as object[];
					return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
				}).ToArray()
			);
		public RedisResult<object> GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUS", key, ""
			.AddIf(true, longitude, latitude, radius, unit)
			.AddIf(withdoord, "WITHCOORD")
			.AddIf(withdist, "WITHDIST")
			.AddIf(withhash, "WITHHASH")
			.AddIf(count != 0, "COUNT", count)
			.AddIf(collation != null, collation)
			.AddIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.AddIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.ToArray());
		public RedisResult<object> GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUSBYMEMBER", key, ""
			.AddIf(true, member, radius, unit)
			.AddIf(withdoord, "WITHCOORD")
			.AddIf(withdist, "WITHDIST")
			.AddIf(withhash, "WITHHASH")
			.AddIf(count != 0, "COUNT", count)
			.AddIf(collation != null, collation)
			.AddIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.AddIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.ToArray());
		#endregion

		#region Commands Hashes
		public RedisResult<long> HDel(string key, params string[] fields) => Call<long>("HDEL", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<bool> HExists(string key, string field) => Call<bool>("HEXISTS", key, field);
		public RedisResult<string> HGet(string key, string field) => Call<string>("HGET", key, field);
		public RedisResult<string[]> HGetAll(string key) => Call<string[]>("HGETALL", key);
		public RedisResult<long> IncrBy(string key, string field, long increment) => Call<long>("HINCRBY", key, field, increment);
		public RedisResult<decimal> IncrByFloat(string key, string field, decimal increment) => Call<decimal>("HINCRBYFLOAT", key, field, increment);
		public RedisResult<string[]> HKeys(string key) => Call<string[]>("HKEYS", key);
		public RedisResult<long> HLen(string key) => Call<long>("HLEN", key);
		public RedisResult<string[]> HMGet(string key, params string[] fields) => Call<string[]>("HMGET", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<string> HMSet(string key, Dictionary<string, string> keyValues) => Call<string>("HMSET", key, keyValues.ToKvArray());
		public RedisResult<ScanValue<string>> HScan(string key, long cursor, string pattern, long count, string type) => Call<object>("HSCAN", key, ""
			.AddIf(true, cursor)
			.AddIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.AddIf(count != 0, "COUNT", count)
			.ToArray()).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<long> HSet(string key, string field, string value) => Call<long>("HSET", key, field, value);
		public RedisResult<long> HSet(string key, Dictionary<string, string> keyValues) => Call<long>("HSET", key, keyValues.ToKvArray());
		public RedisResult<bool> HSetNx(string key, string field, string value) => Call<bool>("HSET", key, field, value);
		public RedisResult<long> HStrLen(string key, string field) => Call<long>("HSTRLEN", key, field);
		public RedisResult<string[]> HVals(string key) => Call<string[]>("HVALS", key);
		#endregion

		#region Commands HyperLogLog
		public RedisResult<bool> PfAdd(string key, params string[] elements) => Call<bool>("PFADD", key, elements);
		public RedisResult<string[]> PfCount(string[] keys) => Call<string[]>("PFCOUNT", null, "".AddIf(keys?.Any() == true, keys).ToArray());
		public RedisResult<string> PfMerge(string destkey, params string[] sourcekeys) => Call<string>("PFMERGE", destkey, "".AddIf(sourcekeys?.Any() == true, sourcekeys).ToArray());
		#endregion

		#region Commands Keys
		public RedisResult<long> Del(params string[] keys) => Call<long>("DEL", null, keys);
		public RedisResult<byte[]> Dump(string key) => Call<byte[]>("DUMP", key);
		public RedisResult<long> Exists(params string[] keys) => Call<long>("EXISTS", null, keys);
		public RedisResult<bool> Expire(string key, int seconds) => Call<bool>("EXPIRE", key, seconds);
		public RedisResult<bool> ExpireAt(string key, DateTime timestamp) => Call<bool>("EXPIREAT", key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		public RedisResult<string[]> Keys(string pattern) => Call<string[]>("KEYS", pattern);
		public RedisResult<string> Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call<string>("MIGRATE", null, ""
			.AddIf(true, host, port, key, destinationDb, timeoutMilliseconds)
			.AddIf(copy, "COPY")
			.AddIf(replace, "REPLACE")
			.AddIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
			.AddIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
			.AddIf(keys?.Any() == true, keys)
			.ToArray());
		public RedisResult<bool> Move(string key, int db) => Call<bool>("MOVE", key, db);
		public RedisResult<long> ObjectRefCount(string key) => Call<long>("OBJECT", "REFCOUNT", key);
		public RedisResult<long> ObjectIdleTime(string key) => Call<long>("OBJECT", "IDLETIME", key);
		public RedisResult<object> ObjectEncoding(string key) => Call<object>("OBJECT", "ENCODING", key);
		public RedisResult<object> ObjectFreq(string key) => Call<object>("OBJECT", "FREQ", key);
		public RedisResult<object> ObjectHelp(string key) => Call<object>("OBJECT", "HELP", key);
		public RedisResult<bool> Presist(string key) => Call<bool>("PERSIST", key);
		public RedisResult<bool> PExpire(string key, int milliseconds) => Call<bool>("PEXPIRE", key, milliseconds);
		public RedisResult<bool> PExpireAt(string key, DateTime timestamp) => Call<bool>("PEXPIREAT", key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
		public RedisResult<long> PTtl(string key) => Call<long>("PTTL", key);
		public RedisResult<string> RandomKey() => Call<string>("RANDOMKEY");
		public RedisResult<string> Rename(string key, string newkey) => Call<string>("RENAME", key, newkey);
		public RedisResult<bool> RenameNx(string key, string newkey) => Call<bool>("RENAMENX", key, newkey);
		public RedisResult<string> Restore(string key, int ttl, byte[] serializedValue, bool replace, bool absTtl, int idleTimeSeconds, decimal frequency) => Call<string>("RENAMENX", key, ""
			.AddIf(true, ttl, serializedValue)
			.AddIf(replace, "REPLACE")
			.AddIf(absTtl, "ABSTTL")
			.AddIf(idleTimeSeconds != 0, "IDLETIME", idleTimeSeconds)
			.AddIf(frequency != 0, "FREQ", frequency)
			.ToArray());
		public RedisResult<ScanValue<string>> Scan(long cursor, string pattern, long count, string type) => Call<object>("SCAN", null, ""
			.AddIf(true, cursor)
			.AddIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.AddIf(count != 0, "COUNT", count)
			.AddIf(!string.IsNullOrWhiteSpace(type), "TYPE", type)
			.ToArray()).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<object> Sort(string key, string ByPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => Call<object>("OBJECT", key, ""
			.AddIf(!string.IsNullOrWhiteSpace(ByPattern), "BY", ByPattern)
			.AddIf(offset != 0 || count != 0, "LIMIT", offset, count)
			.AddIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
			.AddIf(collation != null, collation)
			.AddIf(alpha, "ALPHA")
			.AddIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
			.ToArray());
		public RedisResult<long> Touch(params string[] keys) => Call<long>("TOUCH", null, keys);
		public RedisResult<long> Ttl(string key) => Call<long>("TTL", key);
		public RedisResult<string> Type(string key) => Call<string>("TYPE", key);
		public RedisResult<long> UnLink(params string[] keys) => Call<long>("UNLINK", null, keys);
		public RedisResult<long> Wait(long numreplicas, long timeoutMilliseconds) => Call<long>("WAIT", null, numreplicas, timeoutMilliseconds);

		#endregion

		#region Commands Lists
		public RedisResult<string> BLPop(string key, int timeoutSeconds) => Call<string[]>("BLPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BLPop(string[] keys, int timeoutSeconds) => Call<string[]>("BLPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string> BRPop(string key, int timeoutSeconds) => Call<string[]>("BRPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BRPop(string[] keys, int timeoutSeconds) => Call<string[]>("BRPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string[]> BRPopLPush(string source, string destination, int timeoutSeconds) => Call<string[]>("BRPOPLPUSH", source, destination, timeoutSeconds);
		public RedisResult<string> LIndex(string key, long index) => Call<string>("LINDEX", key, index);
		public RedisResult<long> LInsert(string key, InsertDirection direction, string pivot, string element) => Call<long>("LINSERT", key, direction, pivot, element);
		public RedisResult<long> LLen(string key) => Call<long>("LLEN", key);
		public RedisResult<string> LPop(string key) => Call<string>("LPOP", key);
		public RedisResult<long> LPos(string key, string element, int rank = 0) => Call<long>("LPOS", key, element.AddIf(rank != 0, "RANK", rank).ToArray());
		public RedisResult<long[]> LPos(string key, string element, int rank, int count, int maxLen) => Call<long[]>("LPOS", key, element
			.AddIf(rank != 0, "RANK", rank)
			.AddIf(true, "COUNT", count)
			.AddIf(maxLen != 0, "MAXLEN ", maxLen)
			.ToArray());
		public RedisResult<long> LPush(string key, params string[] elements) => Call<long>("LPUSH", key, elements);
		public RedisResult<long> LPushX(string key, params string[] elements) => Call<long>("LPUSHX", key, elements);
		public RedisResult<string[]> LRange(string key, long start, long stop) => Call<string[]>("LRANGE", key, start, stop);
		public RedisResult<long> LRem(string key, long count, string element) => Call<long>("LREM", key, count, element);
		public RedisResult<string> LSet(string key, long index, string element) => Call<string>("LSET", key, index, element);
		public RedisResult<string[]> LTrim(string key, long start, long stop) => Call<string[]>("LTRIM", key, start, stop);
		public RedisResult<string> RPop(string key) => Call<string>("RPOP", key);
		public RedisResult<string[]> RPopLPush(string source, string destination) => Call<string[]>("RPOPLPUSH", source, destination);
		public RedisResult<long> RPush(string key, params string[] elements) => Call<long>("RPUSH", key, elements);
		public RedisResult<long> RPushX(string key, params string[] elements) => Call<long>("RPUSHX", key, elements);
		#endregion

		#region Commands Scripting
		public RedisResult<object> Eval(string script, string[] keys, params object[] arguments) => Call<object>("EVAL", null, script.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<object> EvalSha(string sha1, string[] keys, params object[] arguments) => Call<object>("EVALSHA", null, sha1.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<string> ScriptDebug(ScriptDebugOption options) => Call<string>("SCRIPT", "DEBUG", options);
		public RedisResult<bool> ScriptExists(string sha1) => Call<bool[]>("SCRIPT", "EXISTS", sha1).NewValue(a => a.FirstOrDefault());
		public RedisResult<bool[]> ScriptExists(string[] sha1) => Call<bool[]>("SCRIPT", "EXISTS", sha1);
		public RedisResult<string> ScriptFlush() => Call<string>("SCRIPT", "FLUSH");
		public RedisResult<string> ScriptKill() => Call<string>("SCRIPT", "KILL");
		public RedisResult<string> ScriptLoad(string script) => Call<string>("SCRIPT", "LOAD", script);
		#endregion

		#region Commands Server
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
		#endregion

		#region Commands Sets
		public RedisResult<long> SAdd(string key, params string[] members) => Call<long>("SADD", key, members);
		public RedisResult<long> SCard(string key) => Call<long>("SCARD", key);
		public RedisResult<string[]> SDiff(params string[] keys) => Call<string[]>("SDIFF", null, keys);
		public RedisResult<long> SDiffStore(string destination, params string[] keys) => Call<long>("SDIFFSTORE", destination, keys);
		public RedisResult<string[]> SInter(params string[] keys) => Call<string[]>("SINTER", null, keys);
		public RedisResult<long> SInterStore(string destination, params string[] keys) => Call<long>("SINTERSTORE", destination, keys);
		public RedisResult<bool> SIsMember(string key, string member) => Call<bool>("SISMEMBER", key, member);
		public RedisResult<string[]> SMeMembers(string key) => Call<string[]>("SMEMBERS", key);
		public RedisResult<bool> SMove(string source, string destination, string member) => Call<bool>("SMOVE", source, destination, member);
		public RedisResult<string> SPop(string key) => Call<string>("SPOP", key);
		public RedisResult<string[]> SPop(string key, int count) => Call<string[]>("SPOP", key, count);
		public RedisResult<string> SRandMember(string key) => Call<string>("SRANDMEMBER", key);
		public RedisResult<string[]> SRandMember(string key, int count) => Call<string[]>("SRANDMEMBER", key, count);
		public RedisResult<long> SRem(string key, params string[] members) => Call<long>("SREM", key, members);
		//SSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<string[]> SUnion(params string[] keys) => Call<string[]>("SUNION", null, keys);
		public RedisResult<long> SUnionStore(string destination, params string[] keys) => Call<long>("SUNIONSTORE", destination, keys);
		#endregion

		#region Commands Sorted Sets
		public RedisResult<SortedSetMember<string>> BZPopMax(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMax(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", keys, timeoutSeconds);
		public RedisResult<SortedSetMember<string>> BZPopMin(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMin(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", keys, timeoutSeconds);
		RedisResult<SortedSetMember<string>> BZPopMaxMin(string command, string key, int timeoutSeconds) => Call<string[]>(command, key, timeoutSeconds).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		/// <summary>
		/// 弹出多个 keys 有序集合值，返回 [] 的下标与之对应
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="timeoutSeconds"></param>
		/// <returns></returns>
		RedisResult<SortedSetMember<string>[]> BZPopMaxMin(string command, string[] keys, int timeoutSeconds)
		{
			return Call<string[]>(command, null, "".AddIf(true, keys, timeoutSeconds).ToArray())
				.NewValue(a =>
				{
					if (a == null) return null;
					var result = new SortedSetMember<string>[keys.Length];
					var oldkeys = keys.ToList();
					for (var z = 0; z < a.Length; z += 3)
					{
						var oldkeysIdx = oldkeys.FindIndex(x => x == a[z]);
						result[oldkeysIdx] = new SortedSetMember<string>(a[z + 1], a[z + 2].ConvertTo<decimal>());
						oldkeys[oldkeysIdx] = null;
					}
					return result;
				});
		}
		public RedisResult<long> ZAdd(string key, decimal score, string member) => ZAdd<long>(key, new[] { new SortedSetMember<string>(member, score) }, false, false, false, false);
		public RedisResult<long> ZAdd(string key, SortedSetMember<string>[] memberScores) => ZAdd<long>(key, memberScores, false, false, false, false);
		public RedisResult<long> ZAdd(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<long>(key, memberScores, nx, xx, ch, false);
		public RedisResult<string[]> ZAddIncr(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<string[]>(key, memberScores, nx, xx, ch, true);
		RedisResult<TReturn> ZAdd<TReturn>(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch, bool incr) => Call<TReturn>("ZADD", key, ""
			.AddIf(nx, "NX")
			.AddIf(xx, "XX")
			.AddIf(ch, "CH")
			.AddIf(incr, "INCR")
			.AddIf(true, memberScores.Select(a => new object[] { a.Score, a.Member }).SelectMany(a => a).ToArray())
			.ToArray());
		public RedisResult<long> ZCard(string key) => Call<long>("ZCARD", key);
		public RedisResult<long> ZCount(string key, decimal min, decimal max) => Call<long>("ZCOUNT", key, min, max);
		public RedisResult<long> ZCount(string key, string min, string max) => Call<long>("ZCOUNT", key, min, max);
		public RedisResult<decimal> ZIncrBy(string key, decimal increment, string member) => Call<decimal>("ZINCRBY", key, increment, member);
		//ZINTERSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		public RedisResult<long> ZLexCount(string key, string min, string max) => Call<long>("ZLEXCOUNT", key, min, max);
		public RedisResult<SortedSetMember<string>> ZPopMin(string key) => ZPopMaxMin("ZPOPMIN", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMin(string key, int count) => ZPopMaxMin("ZPOPMIN", key, count);
		public RedisResult<SortedSetMember<string>> ZPopMax(string key) => ZPopMaxMin("ZPOPMAX", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMax(string key, int count) => ZPopMaxMin("ZPOPMAX", key, count);
		RedisResult<SortedSetMember<string>> ZPopMaxMin(string command, string key) => Call<string[]>(command, key).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		RedisResult<SortedSetMember<string>[]> ZPopMaxMin(string command, string key, int count) => Call<string[]>(command, key, count).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRange(string key, decimal start, decimal stop) => Call<string[]>("ZRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRangeWithScores(string key, decimal start, decimal stop) => Call<string[]>("ZRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRangeByLex(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRank(string key, string member) => Call<long>("ZRANK", key, member);
		public RedisResult<long> ZRem(string key, params string[] members) => Call<long>("ZREM", key, members);
		public RedisResult<long> ZRemRangeByLex(string key, string min, string max) => Call<long>("ZREMRANGEBYLEX", key, min, max);
		public RedisResult<long> ZRemRangeByRank(string key, long start, long stop) => Call<long>("ZREMRANGEBYRANK", key, start, stop);
		public RedisResult<long> ZRemRangeByScore(string key, decimal min, decimal max) => Call<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<long> ZRemRangeByScore(string key, string min, string max) => Call<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRevRange(string key, decimal start, decimal stop) => Call<string[]>("ZREVRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeWithScores(string key, decimal start, decimal stop) => Call<string[]>("ZREVRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRevRank(string key, string member) => Call<long>("ZREVRANK", key, member);
		//ZSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<decimal> ZScore(string key, string member) => Call<decimal>("ZSCORE", key, member);
		//ZUNIONSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		#endregion

		#region Commands Streams
		public RedisResult<long> XAck(string key, string group, params string[] id) => Call<long>("XACK", key, group.AddIf(true, id).ToArray());
		public RedisResult<string> XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => Call<string>("XADD", key, ""
			.AddIf(maxLen > 0, "MAXLEN", maxLen)
			.AddIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.AddIf(true, fieldValues.ToKvArray())
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id)
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "JUSTID")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.AddIf(true, "JUSTID")
			.ToArray());
		public RedisResult<long> XDel(string key, params string[] id) => Call<long>("XDEL", key, id);
		public RedisResult<string> XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call<string>("XGROUP", "CREATE", key
			.AddIf(true, group, id)
			.AddIf(MkStream, "MKSTREAM")
			.ToArray());
		public RedisResult<string> XGroupSetId(string key, string group, string id = "$") => Call<string>("XGROUP", "SETID", key, group, id);
		public RedisResult<bool> XGroupDestroy(string key, string group) => Call<bool>("XGROUP", "DESTROY", key, group);
		public RedisResult<bool> XGroupDelConsumer(string key, string group, string consumer) => Call<bool>("XGROUP", "DELCONSUMER", key, group, consumer);
		public RedisResult<object> XInfoStream(string key) => Call<object>("XINFO", "STREAM", key);
		public RedisResult<object> XInfoGroups(string key) => Call<object>("XINFO", "GROUPS", key);
		public RedisResult<object> XInfoConsumers(string key, string group) => Call<object>("XINFO", "CONSUMERS", key, group);
		public RedisResult<long> XLen(string key) => Call<long>("XLEN", key);
		public RedisResult<object> XPending(string key, string group) => Call<object>("XPENDING", key, group);
		public RedisResult<object> XPending(string key, string group, string start, string end, long count, string consumer = null) => Call<object>("XPENDING", key, group
			.AddIf(true, start, end, count)
			.AddIf(!string.IsNullOrWhiteSpace(consumer), consumer)
			.ToArray());
		public RedisResult<object> XRange(string key, string start, string end, long count = 1) => Call<object>("XRANGE", key, start
			.AddIf(true, end)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRevRange(string key, string end, string start, long count = 1) => Call<object>("XREVRANGE", key, end
			.AddIf(true, start)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string key, string id) => Call<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string[] key, string[] id) => Call<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string key, string id) => Call<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => Call<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<long> XTrim(string key, long maxLen) => Call<long>("XTRIM", key, "MAXLEN", maxLen > 0 ? maxLen.ToString() : $"~{Math.Abs(maxLen)}");
		#endregion

		#region Commands Strings
		public RedisResult<long> Append(string key, object value) => Call<long>("APPEND", key, value);
		public RedisResult<long> BitCount(string key, long start, long end) => Call<long>("BITCOUNT", key, start, end);
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public RedisResult<long> BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call<long>("BITOP", null, "".AddIf(true, operation, destkey, keys).ToArray());
		public RedisResult<long> BitPos(string key, object bit, long start = 0, long end = 0) => start > 0 && end > 0 ? Call<long>("BITPOS", key, new object[] { bit, start, end }) :
			(start > 0 ? Call<long>("BITPOS", key, new object[] { bit, start }) : Call<long>("BITPOS", key, bit));
		public RedisResult<long> Decr(string key) => Call<long>("DECR", key);
		public RedisResult<long> DecrBy(string key, long decrement) => Call<long>("DECRBY", key, decrement);
		public RedisResult<string> Get(string key) => Call<string>("GET", key);
		public RedisResult<long> GetBit(string key, long offset) => Call<long>("GETBIT", key, offset);
		public RedisResult<string> GetRange(string key, long start, long end) => Call<string>("GETRANGE", key, start, end);
		public RedisResult<string> GetSet(string key, object value) => Call<string>("GETSET", key, value);
		public RedisResult<long> Incr(string key) => Call<long>("INCR", key);
		public RedisResult<long> IncrBy(string key, long increment) => Call<long>("INCRBY", key, increment);
		public RedisResult<decimal> IncrByFloat(string key, decimal increment) => Call<decimal>("INCRBYFLOAT", key, increment);
		public RedisResult<string[]> MGet(params string[] keys) => Call<string[]>("MGET", null, keys);
		public RedisResult<string> MSet(Dictionary<string, object> keyValues) => Call<string>("MSET", null, keyValues.ToKvArray());
		public RedisResult<long> MSetNx(Dictionary<string, object> keyValues) => Call<long>("MSETNX", null, keyValues.ToKvArray());
		public RedisResult<string> PSetNx(string key, long milliseconds, object value) => Call<string>("PSETEX", key, milliseconds, value);
		public RedisResult<string> Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call<string>("SET", key, ""
			.AddIf(true, value)
			.AddIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.AddIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.AddIf(keepTtl, "KEEPTTL")
			.AddIf(nx, "NX")
			.AddIf(xx, "XX").ToArray());
		public RedisResult<long> SetBit(string key, long offset, object value) => Call<long>("SETBIT", key, offset, value);
		public RedisResult<string> SetEx(string key, int seconds, object value) => Call<string>("SETEX", key, seconds, value);
		public RedisResult<bool> SetNx(string key, object value) => Call<bool>("SETNX", key, value);
		public RedisResult<long> SetRange(string key, long offset, object value) => Call<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => Call<long>("STRLEN", key);
		#endregion

		#region Commands Transactions
		public RedisResult<string> Discard() => Call<string>("DISCARD");
		public RedisResult<object[]> Exec() => Call<object[]>("EXEC");
		public RedisResult<string> Multi() => Call<string>("MULTI");
		public RedisResult<string> UnWatch() => Call<string>("UNWATCH");
		public RedisResult<string> Watch(params string[] keys) => Call<string>("WATCH", null, keys);
		#endregion

		#region Commands Bloom Filter
		public RedisResult<string> BfReserve(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => Call<string>("BF.RESERVE", key, ""
			.AddIf(true, errorRate, capacity)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(nonScaling, "NONSCALING")
			.ToArray());
		public RedisResult<bool> BfAdd(string key, string item) => Call<bool>("BF.ADD", key, item);
		public RedisResult<bool[]> BfMAdd(string key, string[] items) => Call<bool[]>("BF.MADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> BfInsert(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => Call<string>("BF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(nonScaling, "NONSCALING")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> BfExists(string key, string item) => Call<bool>("BF.EXISTS", key, item);
		public RedisResult<bool[]> BfMExists(string key, string[] items) => Call<bool[]>("BF.MEXISTS", key, "".AddIf(true, items).ToArray());
		public RedisResult<ScanValue<byte[]>> BfScanDump(string key, long iter) => Call<object>("BF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> BfLoadChunk(string key, long iter, byte[] data) => Call<string>("BF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> BfInfo(string key) => Call<string[]>("BF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom Cuckoo Filter
		public RedisResult<string> CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => Call<string>("CF.RESERVE", key, ""
			.AddIf(true, capacity)
			.AddIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
			.AddIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.ToArray());
		public RedisResult<bool> CfAdd(string key, string item) => Call<bool>("CF.ADD", key, item);
		public RedisResult<bool> CfAddNx(string key, string item) => Call<bool>("CF.ADDNX", key, item);
		public RedisResult<string> CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
		public RedisResult<string> CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
		RedisResult<string> CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => Call<string>(nx ? "CF.INSERTNX" : "CF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> CfExists(string key, string item) => Call<bool>("CF.EXISTS", key, item);
		public RedisResult<bool> CfDel(string key, string item) => Call<bool>("CF.DEL", key, item);
		public RedisResult<long> CfCount(string key, string item) => Call<long>("CF.COUNT", key, item);
		public RedisResult<ScanValue<byte[]>> CfScanDump(string key, long iter) => Call<object>("CF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> CfLoadChunk(string key, long iter, byte[] data) => Call<string>("CF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> CfInfo(string key) => Call<string[]>("CF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom Count-Min Sketch
		public RedisResult<string> CmsInitByDim(string key, long width, long depth) => Call<string>("CMS.INITBYDIM", key, width, depth);
		public RedisResult<string> CmsInitByProb(string key, decimal error, decimal probability) => Call<string>("CMS.INITBYPROB", key, error, probability);
		public RedisResult<long> CmsIncrBy(string key, string item, long increment) => Call<long[]>("CMS.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<long[]> CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<long[]>("CMS.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<long[]> CmsQuery(string key, string[] items) => Call<long[]>("CMS.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> CmsMerge(string dest, long numKeys, string[] src, long[] weights) => Call<string>("CMS.MERGE", null, ""
			.AddIf(true, dest, numKeys, src)
			.AddIf(weights?.Any() == true, "WEIGHTS", weights)
			.ToArray());
		public RedisResult<Dictionary<string, string>> CmsInfo(string key) => Call<string[]>("CMS.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom TopK Filter
		public RedisResult<string> TopkReserve(string key, long topk, long width, long depth, decimal decay) => Call<string>("TOPK.RESERVE", key, topk, width, depth, decay);
		public RedisResult<string[]> TopkAdd(string key, string[] items) => Call<string[]>("TOPK.ADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> TopkIncrBy(string key, string item, long increment) => Call<string[]>("TOPK.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => Call<string[]>("TOPK.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<bool[]> TopkQuery(string key, string[] items) => Call<bool[]>("TOPK.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<long[]> TopkCount(string key, string[] items) => Call<long[]>("TOPK.COUNT", key, "".AddIf(true, items).ToArray());
		public RedisResult<string[]> TopkList(string key) => Call<string[]>("TOPK.LIST", key);
		public RedisResult<Dictionary<string, string>> TopkInfo(string key) => Call<string[]>("TOPK.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
        #endregion
    }

	public enum ClusterSetSlotType { Importing, Migrating, Stable, Node }
	public enum ClusterResetType { Hard, Soft }
	public enum ClusterFailOverType { Force, TakeOver }
	public enum ClientUnBlockType { Timeout, Error }
	public enum ClientReplyType { On, Off, Skip }
	public enum ClientType { Normal, Master, Slave, PubSub }
	public enum Confirm { Yes, No }
	public enum GeoUnit { M, KM, MI, FT }
	public enum Collation { Asc, Desc }
	public enum InsertDirection { Before, After }
	public enum ScriptDebugOption { Yes, Sync, No }
	public enum BitOpOperation { And, Or, Xor, Not }
	public class GeoMember
	{
		public decimal Longitude { get; set; }
		public decimal Latitude { get; set; }
		public string Member { get; set; }

		public GeoMember(decimal longitude, decimal latitude, string member) { Longitude = longitude; Latitude = latitude; Member = member; }
	}
	public class ScanValue<T>
	{
		public long Cursor { get; set; }
		public T[] Items { get; set; }
		public ScanValue(long cursor, T[] items) { Cursor = cursor; Items = items; }
	}
	public class SortedSetMember<T>
	{
		public T Member { get; set; }
		public decimal Score { get; set; }
		public SortedSetMember(T member, decimal score) { this.Member = member; this.Score = score; }
	}
}

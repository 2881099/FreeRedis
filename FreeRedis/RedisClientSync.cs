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
		public void SendCommandOnlyWrite(string cmd, string subcmd = null, params object[] parms)
		{
			var args = PrepareCommand(cmd, subcmd, parms);
			Resp3Helper.Write(Stream, Encoding, args, true);
		}
		public void SendCommandListen(Action<object> ondata, Func<bool> next, string command, string subcommand = null, params object[] parms)
		{
			var args = PrepareCommand(command, subcommand, parms);
			Resp3Helper.Write(Stream, args, true);
			_listeningCommand = string.Join(" ", args);
			do
			{
				try
				{
					var data = Resp3Helper.Read<object>(Stream).Value;
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

		#region Commands Cluster
		public RedisResult<string> ClusterAddSlots(params int[] slot) => SendCommand<string>("CLUSTER", "ADDSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterBumpEpoch() => SendCommand<string>("CLUSTER", "BUMPEPOCH");
		public RedisResult<long> ClusterCountFailureReports(string nodeid) => SendCommand<long>("CLUSTER", "COUNT-FAILURE-REPORTS", nodeid);
		public RedisResult<long> ClusterCountKeysInSlot(int slot) => SendCommand<long>("CLUSTER", "COUNTKEYSINSLOT", slot);
		public RedisResult<string> ClusterDelSlots(params int[] slot) => SendCommand<string>("CLUSTER", "DELSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterFailOver(ClusterFailOverType type) => SendCommand<string>("CLUSTER", "FAILOVER", type);
		public RedisResult<string> ClusterFlushSlots() => SendCommand<string>("CLUSTER", "FLUSHSLOTS");
		public RedisResult<long> ClusterForget(string nodeid) => SendCommand<long>("CLUSTER", "FORGET", nodeid);
		public RedisResult<string[]> ClusterGetKeysInSlot(int slot) => SendCommand<string[]>("CLUSTER", "GETKEYSINSLOT", slot);
		public RedisResult<Dictionary<string, string>> ClusterInfo() => SendCommand<string[]>("CLUSTER", "INFO").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<int> ClusterKeySlot(string key) => SendCommand<int>("CLUSTER", "KEYSLOT", key);
		public RedisResult<string> ClusterMeet(string ip, int port) => SendCommand<string>("CLUSTER", "MEET", ip, port);
		public RedisResult<string> ClusterMyId() => SendCommand<string>("CLUSTER", "MYID");
		public RedisResult<string> ClusterNodes() => SendCommand<string>("CLUSTER", "NODES");
		public RedisResult<string> ClusterReplicas(string nodeid) => SendCommand<string>("CLUSTER", "REPLICAS", nodeid);
		public RedisResult<string> ClusterReplicate(string nodeid) => SendCommand<string>("CLUSTER", "REPLICATE", nodeid);
		public RedisResult<string> ClusterReset(ClusterResetType type) => SendCommand<string>("CLUSTER", "RESET", type);
		public RedisResult<string> ClusterSaveConfig() => SendCommand<string>("CLUSTER", "SAVECONFIG");
		public RedisResult<string> ClusterSetConfigEpoch(string epoch) => SendCommand<string>("CLUSTER", "SET-CONFIG-EPOCH", epoch);
		public RedisResult<string[]> ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => SendCommand<string[]>("CLUSTER", "SETSLOT", ""
			.AddIf(true, slot, type)
			.AddIf(!string.IsNullOrWhiteSpace(nodeid), nodeid)
			.ToArray());
		public RedisResult<string> ClusterSlaves(string nodeid) => SendCommand<string>("CLUSTER", "SLAVES", nodeid);
		public RedisResult<object> ClusterSlots() => SendCommand<object>("CLUSTER", "SLOTS");
		public RedisResult<string> ReadOnly() => SendCommand<string>("READONLY");
		public RedisResult<string> ReadWrite() => SendCommand<string>("READWRITE");
		#endregion

		#region Commands Connection
		public RedisResult<string> Auth(string password) => SendCommand<string>("AUTH", null, password);
		public RedisResult<string> Auth(string username, string password) => SendCommand<string>("AUTH", null, ""
			.AddIf(!string.IsNullOrWhiteSpace(username), username)
			.AddIf(true, password)
			.ToArray());
		public RedisResult<string> ClientCaching(Confirm confirm) => SendCommand<string>("CLIENT", "CACHING", confirm);
		public RedisResult<string> ClientGetName() => SendCommand<string>("CLIENT", "GETNAME");
		public RedisResult<long> ClientGetRedir() => SendCommand<long>("CLIENT", "GETREDIR");
		public RedisResult<long> ClientId() => SendCommand<long>("CLIENT", "ID");
		public RedisResult<long> ClientKill(string ipport, long? clientid) => SendCommand<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.ToArray());
		public RedisResult<long> ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => SendCommand<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.AddIf(type != null, "TYPE", type)
			.AddIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.AddIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.AddIf(skipme != null, "SKIPME", skipme)
			.ToArray());
		public RedisResult<string[]> ClientList(ClientType? type) => SendCommand<string[]>("CLIENT", "LIST", ""
			.AddIf(type != null, "TYPE", type)
			.ToArray());
		public RedisResult<string> ClientPaush(long timeoutMilliseconds) => SendCommand<string>("CLIENT", "PAUSE", timeoutMilliseconds);
		public RedisResult<string> ClientReply(ClientReplyType type) => SendCommand<string>("CLIENT", "REPLY", type);
		public RedisResult<string> ClientSetName(string connectionName) => SendCommand<string>("CLIENT", "SETNAME", connectionName);
		public RedisResult<string> ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => SendCommand<string>("CLIENT", "TRACKING", ""
			.AddIf(on_off, "ON")
			.AddIf(!on_off, "OFF")
			.AddIf(redirect != null, "REDIRECT", redirect)
			.AddIf(prefix?.Any() == true, prefix.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.AddIf(bcast, "BCAST")
			.AddIf(optin, "OPTIN")
			.AddIf(optout, "OPTOUT")
			.AddIf(noloop, "NOLOOP")
			.ToArray());
		public RedisResult<bool> ClientUnBlock(long clientid, ClientUnBlockType? type = null) => SendCommand<bool>("CLIENT", "UNBLOCK", ""
			.AddIf(true, clientid)
			.AddIf(type != null, type)
			.ToArray());
		public RedisResult<string> Echo(string message) => SendCommand<string>("ECHO", null, message);
		public RedisResult<object> Hello(decimal protover, string username, string password, string clientname) => SendCommand<object>("HELLO", null, ""
			.AddIf(true, protover)
			.AddIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.AddIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname)
			.ToArray());
		public RedisResult<string> Ping(string message = null) => SendCommand<string>("PING", null, message);
		public RedisResult<string> Quit() => SendCommand<string>("QUIT");
		public RedisResult<string> Select(int index) => SendCommand<string>("SELECT", null, index);
		#endregion

		#region Commands Geo
		public RedisResult<long> GetAdd(string key, params GeoMember[] members) => SendCommand<long>("GEOADD", key, ""
			.AddIf(members?.Any() == true, members.Select(a => new object[] { a.Longitude, a.Latitude, a.Member }).ToArray())
			.ToArray());

		public RedisResult<decimal> GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.M) => SendCommand<decimal>("GEOADD", key, ""
			.AddIf(true, member1, member2)
			.AddIf(unit != GeoUnit.M, unit)
			.ToArray());
		public RedisResult<string[]> GeoHash(string key, string[] members) => SendCommand<string[]>("GEOADD", key, "".AddIf(members?.Any() == true, members).ToArray());
		public RedisResult<GeoMember[]> GeoPos(string key, string[] members) => SendCommand<object>("GEOPOS", key, "".AddIf(members?.Any() == true, members).ToArray())
			.NewValue(a => (a as object[]).Select((z, y) =>
				{
					var zarr = z as object[];
					return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
				}).ToArray()
			);
		public RedisResult<object> GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => SendCommand<object>("GEORADIUS", key, ""
			.AddIf(true, longitude, latitude, radius, unit)
			.AddIf(withdoord, "WITHCOORD")
			.AddIf(withdist, "WITHDIST")
			.AddIf(withhash, "WITHHASH")
			.AddIf(count != 0, "COUNT", count)
			.AddIf(collation != null, collation)
			.AddIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.AddIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.ToArray());
		public RedisResult<object> GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => SendCommand<object>("GEORADIUSBYMEMBER", key, ""
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
		public RedisResult<long> HDel(string key, params string[] fields) => SendCommand<long>("HDEL", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<bool> HExists(string key, string field) => SendCommand<bool>("HEXISTS", key, field);
		public RedisResult<string> HGet(string key, string field) => SendCommand<string>("HGET", key, field);
		public RedisResult<string[]> HGetAll(string key) => SendCommand<string[]>("HGETALL", key);
		public RedisResult<long> IncrBy(string key, string field, long increment) => SendCommand<long>("HINCRBY", key, field, increment);
		public RedisResult<decimal> IncrByFloat(string key, string field, decimal increment) => SendCommand<decimal>("HINCRBYFLOAT", key, field, increment);
		public RedisResult<string[]> HKeys(string key) => SendCommand<string[]>("HKEYS", key);
		public RedisResult<long> HLen(string key) => SendCommand<long>("HLEN", key);
		public RedisResult<string[]> HMGet(string key, params string[] fields) => SendCommand<string[]>("HMGET", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<string> HMSet(string key, Dictionary<string, string> keyValues) => SendCommand<string>("HMSET", key, keyValues.ToKvArray());
		public RedisResult<ScanValue<string>> HScan(string key, long cursor, string pattern, long count, string type) => SendCommand<object>("HSCAN", key, ""
			.AddIf(true, cursor)
			.AddIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.AddIf(count != 0, "COUNT", count)
			.ToArray()).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<long> HSet(string key, string field, string value) => SendCommand<long>("HSET", key, field, value);
		public RedisResult<long> HSet(string key, Dictionary<string, string> keyValues) => SendCommand<long>("HSET", key, keyValues.ToKvArray());
		public RedisResult<bool> HSetNx(string key, string field, string value) => SendCommand<bool>("HSET", key, field, value);
		public RedisResult<long> HStrLen(string key, string field) => SendCommand<long>("HSTRLEN", key, field);
		public RedisResult<string[]> HVals(string key) => SendCommand<string[]>("HVALS", key);
		#endregion

		#region Commands HyperLogLog
		public RedisResult<bool> PfAdd(string key, params string[] elements) => SendCommand<bool>("PFADD", key, elements);
		public RedisResult<string[]> PfCount(string[] keys) => SendCommand<string[]>("PFCOUNT", null, "".AddIf(keys?.Any() == true, keys).ToArray());
		public RedisResult<string> PfMerge(string destkey, params string[] sourcekeys) => SendCommand<string>("PFMERGE", destkey, "".AddIf(sourcekeys?.Any() == true, sourcekeys).ToArray());
		#endregion

		#region Commands Keys
		public RedisResult<long> Del(params string[] keys) => SendCommand<long>("DEL", null, keys);
		public RedisResult<byte[]> Dump(string key) => SendCommand<byte[]>("DUMP", key);
		public RedisResult<long> Exists(params string[] keys) => SendCommand<long>("EXISTS", null, keys);
		public RedisResult<bool> Expire(string key, int seconds) => SendCommand<bool>("EXPIRE", key, seconds);
		public RedisResult<bool> ExpireAt(string key, DateTime timestamp) => SendCommand<bool>("EXPIREAT", key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		public RedisResult<string[]> Keys(string pattern) => SendCommand<string[]>("KEYS", pattern);
		public RedisResult<string> Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => SendCommand<string>("MIGRATE", null, ""
			.AddIf(true, host, port, key, destinationDb, timeoutMilliseconds)
			.AddIf(copy, "COPY")
			.AddIf(replace, "REPLACE")
			.AddIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
			.AddIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
			.AddIf(keys?.Any() == true, keys)
			.ToArray());
		public RedisResult<bool> Move(string key, int db) => SendCommand<bool>("MOVE", key, db);
		public RedisResult<long> ObjectRefCount(string key) => SendCommand<long>("OBJECT", "REFCOUNT", key);
		public RedisResult<long> ObjectIdleTime(string key) => SendCommand<long>("OBJECT", "IDLETIME", key);
		public RedisResult<object> ObjectEncoding(string key) => SendCommand<object>("OBJECT", "ENCODING", key);
		public RedisResult<object> ObjectFreq(string key) => SendCommand<object>("OBJECT", "FREQ", key);
		public RedisResult<object> ObjectHelp(string key) => SendCommand<object>("OBJECT", "HELP", key);
		public RedisResult<bool> Presist(string key) => SendCommand<bool>("PERSIST", key);
		public RedisResult<bool> PExpire(string key, int milliseconds) => SendCommand<bool>("PEXPIRE", key, milliseconds);
		public RedisResult<bool> PExpireAt(string key, DateTime timestamp) => SendCommand<bool>("PEXPIREAT", key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
		public RedisResult<long> PTtl(string key) => SendCommand<long>("PTTL", key);
		public RedisResult<string> RandomKey() => SendCommand<string>("RANDOMKEY");
		public RedisResult<string> Rename(string key, string newkey) => SendCommand<string>("RENAME", key, newkey);
		public RedisResult<bool> RenameNx(string key, string newkey) => SendCommand<bool>("RENAMENX", key, newkey);
		public RedisResult<string> Restore(string key, int ttl, byte[] serializedValue, bool replace, bool absTtl, int idleTimeSeconds, decimal frequency) => SendCommand<string>("RENAMENX", key, ""
			.AddIf(true, ttl, serializedValue)
			.AddIf(replace, "REPLACE")
			.AddIf(absTtl, "ABSTTL")
			.AddIf(idleTimeSeconds != 0, "IDLETIME", idleTimeSeconds)
			.AddIf(frequency != 0, "FREQ", frequency)
			.ToArray());
		public RedisResult<ScanValue<string>> Scan(long cursor, string pattern, long count, string type) => SendCommand<object>("SCAN", null, ""
			.AddIf(true, cursor)
			.AddIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.AddIf(count != 0, "COUNT", count)
			.AddIf(!string.IsNullOrWhiteSpace(type), "TYPE", type)
			.ToArray()).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<object> Sort(string key, string ByPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => SendCommand<object>("OBJECT", key, ""
			.AddIf(!string.IsNullOrWhiteSpace(ByPattern), "BY", ByPattern)
			.AddIf(offset != 0 || count != 0, "LIMIT", offset, count)
			.AddIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
			.AddIf(collation != null, collation)
			.AddIf(alpha, "ALPHA")
			.AddIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
			.ToArray());
		public RedisResult<long> Touch(params string[] keys) => SendCommand<long>("TOUCH", null, keys);
		public RedisResult<long> Ttl(string key) => SendCommand<long>("TTL", key);
		public RedisResult<string> Type(string key) => SendCommand<string>("TYPE", key);
		public RedisResult<long> UnLink(params string[] keys) => SendCommand<long>("UNLINK", null, keys);
		public RedisResult<long> Wait(long numreplicas, long timeoutMilliseconds) => SendCommand<long>("WAIT", null, numreplicas, timeoutMilliseconds);

		#endregion

		#region Commands Lists
		public RedisResult<string> BLPop(string key, int timeoutSeconds) => SendCommand<string[]>("BLPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BLPop(string[] keys, int timeoutSeconds) => SendCommand<string[]>("BLPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string> BRPop(string key, int timeoutSeconds) => SendCommand<string[]>("BRPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BRPop(string[] keys, int timeoutSeconds) => SendCommand<string[]>("BRPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string[]> BRPopLPush(string source, string destination, int timeoutSeconds) => SendCommand<string[]>("BRPOPLPUSH", source, destination, timeoutSeconds);
		public RedisResult<string> LIndex(string key, long index) => SendCommand<string>("LINDEX", key, index);
		public RedisResult<long> LInsert(string key, InsertDirection direction, string pivot, string element) => SendCommand<long>("LINSERT", key, direction, pivot, element);
		public RedisResult<long> LLen(string key) => SendCommand<long>("LLEN", key);
		public RedisResult<string> LPop(string key) => SendCommand<string>("LPOP", key);
		public RedisResult<long> LPos(string key, string element, int rank = 0) => SendCommand<long>("LPOS", key, element.AddIf(rank != 0, "RANK", rank).ToArray());
		public RedisResult<long[]> LPos(string key, string element, int rank, int count, int maxLen) => SendCommand<long[]>("LPOS", key, element
			.AddIf(rank != 0, "RANK", rank)
			.AddIf(true, "COUNT", count)
			.AddIf(maxLen != 0, "MAXLEN ", maxLen)
			.ToArray());
		public RedisResult<long> LPush(string key, params string[] elements) => SendCommand<long>("LPUSH", key, elements);
		public RedisResult<long> LPushX(string key, params string[] elements) => SendCommand<long>("LPUSHX", key, elements);
		public RedisResult<string[]> LRange(string key, long start, long stop) => SendCommand<string[]>("LRANGE", key, start, stop);
		public RedisResult<long> LRem(string key, long count, string element) => SendCommand<long>("LREM", key, count, element);
		public RedisResult<string> LSet(string key, long index, string element) => SendCommand<string>("LSET", key, index, element);
		public RedisResult<string[]> LTrim(string key, long start, long stop) => SendCommand<string[]>("LTRIM", key, start, stop);
		public RedisResult<string> RPop(string key) => SendCommand<string>("RPOP", key);
		public RedisResult<string[]> RPopLPush(string source, string destination) => SendCommand<string[]>("RPOPLPUSH", source, destination);
		public RedisResult<long> RPush(string key, params string[] elements) => SendCommand<long>("RPUSH", key, elements);
		public RedisResult<long> RPushX(string key, params string[] elements) => SendCommand<long>("RPUSHX", key, elements);
		#endregion

		#region Commands Pub/Sub
		public void PSubscribe(string pattern, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_listeningCommand)) SendCommandListen(onData, () => IsConnected, "PSUBSCRIBE", null, pattern);
			else SendCommandOnlyWrite("PSUBSCRIBE", null, pattern);
		}
		public void PSubscribe(string[] pattern, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_listeningCommand)) SendCommandListen(onData, () => IsConnected, "PSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
			else SendCommandOnlyWrite("PSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
		}
		public RedisResult<long> Publish(string channel, string message) => SendCommand<long>("PUBLISH", channel, message);
		public RedisResult<string[]> PubSubChannels(string pattern) => SendCommand<string[]>("PUBSUB", "CHANNELS", pattern);
		public RedisResult<string[]> PubSubNumSub(params string[] channels) => SendCommand<string[]>("PUBSUB", "NUMSUB", "".AddIf(true, channels).ToArray());
		public RedisResult<long> PubSubNumPat() => SendCommand<long>("PUBLISH", "NUMPAT");
		public void PUnSubscribe(params string[] pattern) => SendCommandOnlyWrite("PUNSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
		public void Subscribe(string channel, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_listeningCommand)) SendCommandListen(onData, () => IsConnected, "SUBSCRIBE", null, channel);
			else SendCommandOnlyWrite("SUBSCRIBE", null, channel);
		}
		public void Subscribe(string[] channels, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_listeningCommand)) SendCommandListen(onData, () => IsConnected, "SUBSCRIBE", null, "".AddIf(true, channels).ToArray());
			else SendCommandOnlyWrite("SUBSCRIBE", null, "".AddIf(true, channels).ToArray());
		}
		public void UnSubscribe(params string[] channels) => SendCommandOnlyWrite("UNSUBSCRIBE", null, "".AddIf(true, channels).ToArray());
		#endregion

		#region Commands Scripting
		public RedisResult<object> Eval(string script, string[] keys, params object[] arguments) => SendCommand<object>("EVAL", null, script.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<object> EvalSha(string sha1, string[] keys, params object[] arguments) => SendCommand<object>("EVALSHA", null, sha1.AddIf(true, keys.Length, keys, arguments).ToArray());
		public RedisResult<string> ScriptDebug(ScriptDebugOption options) => SendCommand<string>("SCRIPT", "DEBUG", options);
		public RedisResult<bool> ScriptExists(string sha1) => SendCommand<bool[]>("SCRIPT", "EXISTS", sha1).NewValue(a => a.FirstOrDefault());
		public RedisResult<bool[]> ScriptExists(string[] sha1) => SendCommand<bool[]>("SCRIPT", "EXISTS", sha1);
		public RedisResult<string> ScriptFlush() => SendCommand<string>("SCRIPT", "FLUSH");
		public RedisResult<string> ScriptKill() => SendCommand<string>("SCRIPT", "KILL");
		public RedisResult<string> ScriptLoad(string script) => SendCommand<string>("SCRIPT", "LOAD", script);
		#endregion

		#region Commands Server
		public RedisResult<string[]> AclCat(string categoryname = null) => string.IsNullOrWhiteSpace(categoryname) ? SendCommand<string[]>("ACL", "CAT") : SendCommand<string[]>("ACL", "CAT", categoryname);
		public RedisResult<long> AclDelUser(params string[] username) => username?.Any() == true ? SendCommand<long>("ACL", "DELUSER", username) : throw new ArgumentException(nameof(username));
		public RedisResult<string> AclGenPass(int bits = 0) => bits <= 0 ? SendCommand<string>("ACL", "GENPASS") : SendCommand<string>("ACL", "GENPASS", bits);
		public RedisResult<object> AclGetUser(string username = "default") => SendCommand<object>("ACL", "GETUSER", username);
		public RedisResult<object> AclHelp() => SendCommand<object>("ACL", "HELP");
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
		public RedisResult<long> CommandCount() => SendCommand<long>("COMMAND", "COUNT");
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
		public RedisResult<string> ModuleLoad(string path, params string[] args) => SendCommand<string>("MODULE", "LOAD", path.AddIf(args?.Any() == true, args).ToArray());
		public RedisResult<string> ModuleUnload(string name) => SendCommand<string>("MODULE", "UNLOAD", name);
		public void Monitor(Action<object> onData) => SendCommandListen(onData, () => IsConnected, "MONITOR");
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
		#endregion

		#region Commands Sets
		public RedisResult<long> SAdd(string key, params string[] members) => SendCommand<long>("SADD", key, members);
		public RedisResult<long> SCard(string key) => SendCommand<long>("SCARD", key);
		public RedisResult<string[]> SDiff(params string[] keys) => SendCommand<string[]>("SDIFF", null, keys);
		public RedisResult<long> SDiffStore(string destination, params string[] keys) => SendCommand<long>("SDIFFSTORE", destination, keys);
		public RedisResult<string[]> SInter(params string[] keys) => SendCommand<string[]>("SINTER", null, keys);
		public RedisResult<long> SInterStore(string destination, params string[] keys) => SendCommand<long>("SINTERSTORE", destination, keys);
		public RedisResult<bool> SIsMember(string key, string member) => SendCommand<bool>("SISMEMBER", key, member);
		public RedisResult<string[]> SMeMembers(string key) => SendCommand<string[]>("SMEMBERS", key);
		public RedisResult<bool> SMove(string source, string destination, string member) => SendCommand<bool>("SMOVE", source, destination, member);
		public RedisResult<string> SPop(string key) => SendCommand<string>("SPOP", key);
		public RedisResult<string[]> SPop(string key, int count) => SendCommand<string[]>("SPOP", key, count);
		public RedisResult<string> SRandMember(string key) => SendCommand<string>("SRANDMEMBER", key);
		public RedisResult<string[]> SRandMember(string key, int count) => SendCommand<string[]>("SRANDMEMBER", key, count);
		public RedisResult<long> SRem(string key, params string[] members) => SendCommand<long>("SREM", key, members);
		//SSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<string[]> SUnion(params string[] keys) => SendCommand<string[]>("SUNION", null, keys);
		public RedisResult<long> SUnionStore(string destination, params string[] keys) => SendCommand<long>("SUNIONSTORE", destination, keys);
		#endregion

		#region Commands Sorted Sets
		public RedisResult<SortedSetMember<string>> BZPopMax(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMax(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", keys, timeoutSeconds);
		public RedisResult<SortedSetMember<string>> BZPopMin(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMin(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", keys, timeoutSeconds);
		RedisResult<SortedSetMember<string>> BZPopMaxMin(string command, string key, int timeoutSeconds) => SendCommand<string[]>(command, key, timeoutSeconds).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		/// <summary>
		/// 弹出多个 keys 有序集合值，返回 [] 的下标与之对应
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="timeoutSeconds"></param>
		/// <returns></returns>
		RedisResult<SortedSetMember<string>[]> BZPopMaxMin(string command, string[] keys, int timeoutSeconds)
		{
			return SendCommand<string[]>(command, null, "".AddIf(true, keys, timeoutSeconds).ToArray())
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
		RedisResult<TReturn> ZAdd<TReturn>(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch, bool incr) => SendCommand<TReturn>("ZADD", key, ""
			.AddIf(nx, "NX")
			.AddIf(xx, "XX")
			.AddIf(ch, "CH")
			.AddIf(incr, "INCR")
			.AddIf(true, memberScores.Select(a => new object[] { a.Score, a.Member }).SelectMany(a => a).ToArray())
			.ToArray());
		public RedisResult<long> ZCard(string key) => SendCommand<long>("ZCARD", key);
		public RedisResult<long> ZCount(string key, decimal min, decimal max) => SendCommand<long>("ZCOUNT", key, min, max);
		public RedisResult<long> ZCount(string key, string min, string max) => SendCommand<long>("ZCOUNT", key, min, max);
		public RedisResult<decimal> ZIncrBy(string key, decimal increment, string member) => SendCommand<decimal>("ZINCRBY", key, increment, member);
		//ZINTERSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		public RedisResult<long> ZLexCount(string key, string min, string max) => SendCommand<long>("ZLEXCOUNT", key, min, max);
		public RedisResult<SortedSetMember<string>> ZPopMin(string key) => ZPopMaxMin("ZPOPMIN", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMin(string key, int count) => ZPopMaxMin("ZPOPMIN", key, count);
		public RedisResult<SortedSetMember<string>> ZPopMax(string key) => ZPopMaxMin("ZPOPMAX", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMax(string key, int count) => ZPopMaxMin("ZPOPMAX", key, count);
		RedisResult<SortedSetMember<string>> ZPopMaxMin(string command, string key) => SendCommand<string[]>(command, key).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		RedisResult<SortedSetMember<string>[]> ZPopMaxMin(string command, string key, int count) => SendCommand<string[]>(command, key, count).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRange(string key, decimal start, decimal stop) => SendCommand<string[]>("ZRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRangeWithScores(string key, decimal start, decimal stop) => SendCommand<string[]>("ZRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRangeByLex(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRank(string key, string member) => SendCommand<long>("ZRANK", key, member);
		public RedisResult<long> ZRem(string key, params string[] members) => SendCommand<long>("ZREM", key, members);
		public RedisResult<long> ZRemRangeByLex(string key, string min, string max) => SendCommand<long>("ZREMRANGEBYLEX", key, min, max);
		public RedisResult<long> ZRemRangeByRank(string key, long start, long stop) => SendCommand<long>("ZREMRANGEBYRANK", key, start, stop);
		public RedisResult<long> ZRemRangeByScore(string key, decimal min, decimal max) => SendCommand<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<long> ZRemRangeByScore(string key, string min, string max) => SendCommand<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRevRange(string key, decimal start, decimal stop) => SendCommand<string[]>("ZREVRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeWithScores(string key, decimal start, decimal stop) => SendCommand<string[]>("ZREVRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRevRank(string key, string member) => SendCommand<long>("ZREVRANK", key, member);
		//ZSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<decimal> ZScore(string key, string member) => SendCommand<decimal>("ZSCORE", key, member);
		//ZUNIONSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		#endregion

		#region Commands Streams
		public RedisResult<long> XAck(string key, string group, params string[] id) => SendCommand<long>("XACK", key, group.AddIf(true, id).ToArray());
		public RedisResult<string> XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => SendCommand<string>("XADD", key, ""
			.AddIf(maxLen > 0, "MAXLEN", maxLen)
			.AddIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.AddIf(true, fieldValues.ToKvArray())
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => SendCommand<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id)
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => SendCommand<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => SendCommand<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "JUSTID")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => SendCommand<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.AddIf(true, "JUSTID")
			.ToArray());
		public RedisResult<long> XDel(string key, params string[] id) => SendCommand<long>("XDEL", key, id);
		public RedisResult<string> XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => SendCommand<string>("XGROUP", "CREATE", key
			.AddIf(true, group, id)
			.AddIf(MkStream, "MKSTREAM")
			.ToArray());
		public RedisResult<string> XGroupSetId(string key, string group, string id = "$") => SendCommand<string>("XGROUP", "SETID", key, group, id);
		public RedisResult<bool> XGroupDestroy(string key, string group) => SendCommand<bool>("XGROUP", "DESTROY", key, group);
		public RedisResult<bool> XGroupDelConsumer(string key, string group, string consumer) => SendCommand<bool>("XGROUP", "DELCONSUMER", key, group, consumer);
		public RedisResult<object> XInfoStream(string key) => SendCommand<object>("XINFO", "STREAM", key);
		public RedisResult<object> XInfoGroups(string key) => SendCommand<object>("XINFO", "GROUPS", key);
		public RedisResult<object> XInfoConsumers(string key, string group) => SendCommand<object>("XINFO", "CONSUMERS", key, group);
		public RedisResult<long> XLen(string key) => SendCommand<long>("XLEN", key);
		public RedisResult<object> XPending(string key, string group) => SendCommand<object>("XPENDING", key, group);
		public RedisResult<object> XPending(string key, string group, string start, string end, long count, string consumer = null) => SendCommand<object>("XPENDING", key, group
			.AddIf(true, start, end, count)
			.AddIf(!string.IsNullOrWhiteSpace(consumer), consumer)
			.ToArray());
		public RedisResult<object> XRange(string key, string start, string end, long count = 1) => SendCommand<object>("XRANGE", key, start
			.AddIf(true, end)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRevRange(string key, string end, string start, long count = 1) => SendCommand<object>("XREVRANGE", key, end
			.AddIf(true, start)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string key, string id) => SendCommand<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string[] key, string[] id) => SendCommand<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string key, string id) => SendCommand<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => SendCommand<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<long> XTrim(string key, long maxLen) => SendCommand<long>("XTRIM", key, "MAXLEN", maxLen > 0 ? maxLen.ToString() : $"~{Math.Abs(maxLen)}");
		#endregion

		#region Commands Strings
		public RedisResult<long> Append(string key, object value) => SendCommand<long>("APPEND", key, value);
		public RedisResult<long> BitCount(string key, long start, long end) => SendCommand<long>("BITCOUNT", key, start, end);
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public RedisResult<long> BitOp(BitOpOperation operation, string destkey, params string[] keys) => SendCommand<long>("BITOP", null, "".AddIf(true, operation, destkey, keys).ToArray());
		public RedisResult<long> BitPos(string key, object bit, long start = 0, long end = 0) => start > 0 && end > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start, end }) :
			(start > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start }) : SendCommand<long>("BITPOS", key, bit));
		public RedisResult<long> Decr(string key) => SendCommand<long>("DECR", key);
		public RedisResult<long> DecrBy(string key, long decrement) => SendCommand<long>("DECRBY", key, decrement);
		public RedisResult<string> Get(string key) => SendCommand<string>("GET", key);
		public RedisResult<long> GetBit(string key, long offset) => SendCommand<long>("GETBIT", key, offset);
		public RedisResult<string> GetRange(string key, long start, long end) => SendCommand<string>("GETRANGE", key, start, end);
		public RedisResult<string> GetSet(string key, object value) => SendCommand<string>("GETSET", key, value);
		public RedisResult<long> Incr(string key) => SendCommand<long>("INCR", key);
		public RedisResult<long> IncrBy(string key, long increment) => SendCommand<long>("INCRBY", key, increment);
		public RedisResult<decimal> IncrByFloat(string key, decimal increment) => SendCommand<decimal>("INCRBYFLOAT", key, increment);
		public RedisResult<string[]> MGet(params string[] keys) => SendCommand<string[]>("MGET", null, keys);
		public RedisResult<string> MSet(Dictionary<string, object> keyValues) => SendCommand<string>("MSET", null, keyValues.ToKvArray());
		public RedisResult<long> MSetNx(Dictionary<string, object> keyValues) => SendCommand<long>("MSETNX", null, keyValues.ToKvArray());
		public RedisResult<string> PSetNx(string key, long milliseconds, object value) => SendCommand<string>("PSETEX", key, milliseconds, value);
		public RedisResult<string> Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => SendCommand<string>("SET", key, ""
			.AddIf(true, value)
			.AddIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.AddIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.AddIf(keepTtl, "KEEPTTL")
			.AddIf(nx, "NX")
			.AddIf(xx, "XX").ToArray());
		public RedisResult<long> SetBit(string key, long offset, object value) => SendCommand<long>("SETBIT", key, offset, value);
		public RedisResult<string> SetEx(string key, int seconds, object value) => SendCommand<string>("SETEX", key, seconds, value);
		public RedisResult<bool> SetNx(string key, object value) => SendCommand<bool>("SETNX", key, value);
		public RedisResult<long> SetRange(string key, long offset, object value) => SendCommand<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => SendCommand<long>("STRLEN", key);
		#endregion

		#region Commands Transactions
		public RedisResult<string> Discard() => SendCommand<string>("DISCARD");
		public RedisResult<object[]> Exec() => SendCommand<object[]>("EXEC");
		public RedisResult<string> Multi() => SendCommand<string>("MULTI");
		public RedisResult<string> UnWatch() => SendCommand<string>("UNWATCH");
		public RedisResult<string> Watch(params string[] keys) => SendCommand<string>("WATCH", null, keys);
		#endregion

		#region Commands Bloom Filter
		public RedisResult<string> BfReserve(string key, decimal errorRate, long capacity, int expansion = 2, bool nonScaling = false) => SendCommand<string>("BF.RESERVE", key, ""
			.AddIf(true, errorRate, capacity)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(nonScaling, "NONSCALING")
			.ToArray());
		public RedisResult<bool> BfAdd(string key, string item) => SendCommand<bool>("BF.ADD", key, item);
		public RedisResult<bool[]> BfMAdd(string key, string[] items) => SendCommand<bool[]>("BF.MADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> BfInsert(string key, string[] items, long? capacity = null, string error = null, int expansion = 2, bool noCreate = false, bool nonScaling = false) => SendCommand<string>("BF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(!string.IsNullOrWhiteSpace(error), "ERROR", error)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(nonScaling, "NONSCALING")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> BfExists(string key, string item) => SendCommand<bool>("BF.EXISTS", key, item);
		public RedisResult<bool[]> BfMExists(string key, string[] items) => SendCommand<bool[]>("BF.MEXISTS", key, "".AddIf(true, items).ToArray());
		public RedisResult<ScanValue<byte[]>> BfScanDump(string key, long iter) => SendCommand<object>("BF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> BfLoadChunk(string key, long iter, byte[] data) => SendCommand<string>("BF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> BfInfo(string key) => SendCommand<string[]>("BF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom Cuckoo Filter
		public RedisResult<string> CfReserve(string key, long capacity, long? bucketSize = null, long? maxIterations = null, int? expansion = null) => SendCommand<string>("CF.RESERVE", key, ""
			.AddIf(true, capacity)
			.AddIf(bucketSize != 2, "BUCKETSIZE", bucketSize)
			.AddIf(maxIterations != 2, "MAXITERATIONS", maxIterations)
			.AddIf(expansion != 2, "EXPANSION", expansion)
			.ToArray());
		public RedisResult<bool> CfAdd(string key, string item) => SendCommand<bool>("CF.ADD", key, item);
		public RedisResult<bool> CfAddNx(string key, string item) => SendCommand<bool>("CF.ADDNX", key, item);
		public RedisResult<string> CfInsert(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(false, key, items, capacity, noCreate);
		public RedisResult<string> CfInsertNx(string key, string[] items, long? capacity = null, bool noCreate = false) => CfInsert(true, key, items, capacity, noCreate);
		RedisResult<string> CfInsert(bool nx, string key, string[] items, long? capacity = null, bool noCreate = false) => SendCommand<string>(nx ? "CF.INSERTNX" : "CF.INSERT", key, ""
			.AddIf(capacity != null, "CAPACITY", capacity)
			.AddIf(noCreate, "NOCREATE")
			.AddIf(true, "ITEMS", items)
			.ToArray());
		public RedisResult<bool> CfExists(string key, string item) => SendCommand<bool>("CF.EXISTS", key, item);
		public RedisResult<bool> CfDel(string key, string item) => SendCommand<bool>("CF.DEL", key, item);
		public RedisResult<long> CfCount(string key, string item) => SendCommand<long>("CF.COUNT", key, item);
		public RedisResult<ScanValue<byte[]>> CfScanDump(string key, long iter) => SendCommand<object>("CF.SCANDUMP", key, iter)
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<byte[]>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<byte[][]>());
			});
		public RedisResult<string> CfLoadChunk(string key, long iter, byte[] data) => SendCommand<string>("CF.LOADCHUNK", key, iter, data);
		public RedisResult<Dictionary<string, string>> CfInfo(string key) => SendCommand<string[]>("CF.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom Count-Min Sketch
		public RedisResult<string> CmsInitByDim(string key, long width, long depth) => SendCommand<string>("CMS.INITBYDIM", key, width, depth);
		public RedisResult<string> CmsInitByProb(string key, decimal error, decimal probability) => SendCommand<string>("CMS.INITBYPROB", key, error, probability);
		public RedisResult<long> CmsIncrBy(string key, string item, long increment) => SendCommand<long[]>("CMS.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<long[]> CmsIncrBy(string key, Dictionary<string, long> itemIncrements) => SendCommand<long[]>("CMS.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<long[]> CmsQuery(string key, string[] items) => SendCommand<long[]>("CMS.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> CmsMerge(string dest, long numKeys, string[] src, long[] weights) => SendCommand<string>("CMS.MERGE", null, ""
			.AddIf(true, dest, numKeys, src)
			.AddIf(weights?.Any() == true, "WEIGHTS", weights)
			.ToArray());
		public RedisResult<Dictionary<string, string>> CmsInfo(string key) => SendCommand<string[]>("CMS.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
		#endregion

		#region Commands RedisBloom TopK Filter
		public RedisResult<string> TopkReserve(string key, long topk, long width, long depth, decimal decay) => SendCommand<string>("TOPK.RESERVE", key, topk, width, depth, decay);
		public RedisResult<string[]> TopkAdd(string key, string[] items) => SendCommand<string[]>("TOPK.ADD", key, "".AddIf(true, items).ToArray());
		public RedisResult<string> TopkIncrBy(string key, string item, long increment) => SendCommand<string[]>("TOPK.INCRBY", key, item, increment).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> TopkIncrBy(string key, Dictionary<string, long> itemIncrements) => SendCommand<string[]>("TOPK.INCRBY", key, itemIncrements.ToKvArray());
		public RedisResult<bool[]> TopkQuery(string key, string[] items) => SendCommand<bool[]>("TOPK.QUERY", key, "".AddIf(true, items).ToArray());
		public RedisResult<long[]> TopkCount(string key, string[] items) => SendCommand<long[]>("TOPK.COUNT", key, "".AddIf(true, items).ToArray());
		public RedisResult<string[]> TopkList(string key) => SendCommand<string[]>("TOPK.LIST", key);
		public RedisResult<Dictionary<string, string>> TopkInfo(string key) => SendCommand<string[]>("TOPK.INFO", key).NewValue(a => a.MapToHash<string>(Encoding));
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

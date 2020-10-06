using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
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
		public RedisResult<object> Sort(string key, string byPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => Call<object>("OBJECT", key, ""
			.AddIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
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
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long Del(params string[] keys) => Call<long>("DEL".Input(keys).FlagKey(keys)).ThrowOrValue();
		public RedisResult<byte[]> Dump(string key) => Call<byte[]>("DUMP".Input(key).FlagKey(key));
		public RedisResult<long> Exists(params string[] keys) => Call<long>("EXISTS".Input(keys).FlagKey(keys));
		public RedisResult<bool> Expire(string key, int seconds) => Call<bool>("EXPIRE".Input(key, seconds).FlagKey(key));
		public RedisResult<bool> ExpireAt(string key, DateTime timestamp) => Call<bool>("EXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).FlagKey(key));
		public RedisResult<string[]> Keys(string pattern) => Call<string[]>("KEYS".Input(pattern));
		public RedisResult<string> Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call<string>("MIGRATE"
			.Input(host, port)
			.Input(key, destinationDb, timeoutMilliseconds)
			.InputIf(copy, "COPY")
			.InputIf(replace, "REPLACE")
			.InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
			.InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
			.InputIf(keys?.Any() == true, keys)
			.FlagKey(key)
			.FlagKey(keys));
		public RedisResult<bool> Move(string key, int db) => Call<bool>("MOVE".Input(key, db).FlagKey(key));
		public RedisResult<long> ObjectRefCount(string key) => Call<long>("OBJECT".SubCommand( "REFCOUNT").Input(key).FlagKey(key));
		public RedisResult<long> ObjectIdleTime(string key) => Call<long>("OBJECT".SubCommand("IDLETIME").Input(key).FlagKey(key));
		public RedisResult<object> ObjectEncoding(string key) => Call<object>("OBJECT".SubCommand("ENCODING").Input(key).FlagKey(key));
		public RedisResult<object> ObjectFreq(string key) => Call<object>("OBJECT".SubCommand("FREQ").Input(key).FlagKey(key));
		public RedisResult<object> ObjectHelp(string key) => Call<object>("OBJECT".SubCommand("HELP").Input(key).FlagKey(key));
		public RedisResult<bool> Presist(string key) => Call<bool>("PERSIST".Input(key).FlagKey(key));
		public RedisResult<bool> PExpire(string key, int milliseconds) => Call<bool>("PEXPIRE".Input(key, milliseconds).FlagKey(key));
		public RedisResult<bool> PExpireAt(string key, DateTime timestamp) => Call<bool>("PEXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).FlagKey(key));
		public RedisResult<long> PTtl(string key) => Call<long>("PTTL".Input(key).FlagKey(key));
		public RedisResult<string> RandomKey() => Call<string>("RANDOMKEY");
		public RedisResult<string> Rename(string key, string newkey) => Call<string>("RENAME".Input(key, newkey).FlagKey(key, newkey));
		public RedisResult<bool> RenameNx(string key, string newkey) => Call<bool>("RENAMENX".Input(key, newkey).FlagKey(key, newkey));
		public RedisResult<string> Restore(string key, int ttl, byte[] serializedValue, bool replace, bool absTtl, int idleTimeSeconds, decimal frequency) => Call<string>("RENAMENX"
			.Input(key, ttl)
			.InputRaw(serializedValue)
			.InputIf(replace, "REPLACE")
			.InputIf(absTtl, "ABSTTL")
			.InputIf(idleTimeSeconds != 0, "IDLETIME", idleTimeSeconds)
			.InputIf(frequency != 0, "FREQ", frequency)
			.FlagKey(key));
		public RedisResult<ScanValue<string>> Scan(long cursor, string pattern, long count, string type) => Call<object>("SCAN"
			.Input(cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count != 0, "COUNT", count)
			.InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type)).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<object> Sort(string key, string byPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => Call<object>("OBJECT"
			.Input(key)
			.InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
			.InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
			.InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
			.InputIf(collation != null, collation)
			.InputIf(alpha, "ALPHA")
			.InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
			.FlagKey(key, storeDestination));
		public RedisResult<long> Touch(params string[] keys) => Call<long>("TOUCH".Input(keys).FlagKey(keys));
		public RedisResult<long> Ttl(string key) => Call<long>("TTL".Input(key).FlagKey(key));
		public RedisResult<string> Type(string key) => Call<string>("TYPE".Input(key).FlagKey(key));
		public RedisResult<long> UnLink(params string[] keys) => Call<long>("UNLINK".Input(keys).FlagKey(keys));
		public RedisResult<long> Wait(long numreplicas, long timeoutMilliseconds) => Call<long>("WAIT".Input(numreplicas, timeoutMilliseconds));
    }
}

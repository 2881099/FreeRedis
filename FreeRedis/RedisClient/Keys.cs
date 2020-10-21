using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long Del(params string[] keys) => Call<long>("DEL".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public byte[] Dump(string key) => Call<byte[]>("DUMP".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public bool Exists(string key) => Call<bool>("EXISTS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long Exists(string[] keys) => Call<long>("EXISTS".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public bool Expire(string key, int seconds) => Call<bool>("EXPIRE".Input(key, seconds).FlagKey(key), rt => rt.ThrowOrValue());
		public bool ExpireAt(string key, DateTime timestamp) => Call<bool>("EXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] Keys(string pattern) => Call<string[]>("KEYS".Input(pattern), rt => rt.ThrowOrValue());
		public void Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call<string>("MIGRATE"
			.Input(host, port)
			.Input(key ?? "", destinationDb, timeoutMilliseconds)
			.InputIf(copy, "COPY")
			.InputIf(replace, "REPLACE")
			.InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
			.InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
			.InputIf(keys?.Any() == true, keys)
			.FlagKey(key)
			.FlagKey(keys), rt => rt.ThrowOrValue());
		public bool Move(string key, int db) => Call<bool>("MOVE".Input(key, db).FlagKey(key), rt => rt.ThrowOrValue());
		public long? ObjectRefCount(string key) => Call<long?>("OBJECT".SubCommand( "REFCOUNT").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long ObjectIdleTime(string key) => Call<long>("OBJECT".SubCommand("IDLETIME").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public object ObjectEncoding(string key) => Call<object>("OBJECT".SubCommand("ENCODING").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public object ObjectFreq(string key) => Call<object>("OBJECT".SubCommand("FREQ").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public object ObjectHelp(string key) => Call<object>("OBJECT".SubCommand("HELP").Input(key).FlagKey(key), rt => rt.ThrowOrValue());

		public bool Persist(string key) => Call<bool>("PERSIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public bool PExpire(string key, int milliseconds) => Call<bool>("PEXPIRE".Input(key, milliseconds).FlagKey(key), rt => rt.ThrowOrValue());
		public bool PExpireAt(string key, DateTime timestamp) => Call<bool>("PEXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).FlagKey(key), rt => rt.ThrowOrValue());
		public long PTtl(string key) => Call<long>("PTTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public string RandomKey() => Call<string>("RANDOMKEY", rt => rt.ThrowOrValue());
		public void Rename(string key, string newkey) => Call<string>("RENAME".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue());
		public bool RenameNx(string key, string newkey) => Call<bool>("RENAMENX".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue());
		public void Restore(string key, byte[] serializedValue) => Restore(key, 0, serializedValue);
		public void Restore(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => Call<string>("RESTORE"
			.Input(key, ttl)
			.InputRaw(serializedValue)
			.InputIf(replace, "REPLACE")
			.InputIf(absTtl, "ABSTTL")
			.InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
			.InputIf(frequency != null, "FREQ", frequency)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public ScanResult<string> Scan(long cursor, string pattern, long count, string type) => Call<object, ScanResult<string>>("SCAN"
			.Input(cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count > 0, "COUNT", count)
			.InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
			.NewValue(a =>
			{
				var arr = a as List<object>;
				return new ScanResult<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			}).ThrowOrValue());
		public object Sort(string key, string byPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => Call<object>("SORT"
			.Input(key)
			.InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
			.InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
			.InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
			.InputIf(collation != null, collation)
			.InputIf(alpha, "ALPHA")
			.InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
			.FlagKey(key, storeDestination), rt => rt.ThrowOrValue());
		public long Touch(params string[] keys) => Call<long>("TOUCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public long Ttl(string key) => Call<long>("TTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public KeyType Type(string key) => Call<KeyType>("TYPE".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long UnLink(params string[] keys) => Call<long>("UNLINK".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public long Wait(long numreplicas, long timeoutMilliseconds) => Call<long>("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue());
	}
}

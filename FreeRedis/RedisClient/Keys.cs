using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public long Del(params string[] keys) => Call("DEL".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public byte[] Dump(string key) => Call<byte[], byte[]>("DUMP".Input(key).FlagKey(key), rt => rt.ThrowOrValue<byte[]>());
        public bool Exists(string key) => Call("EXISTS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long Exists(string[] keys) => Call("EXISTS".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public bool Expire(string key, int seconds) => Call("EXPIRE".Input(key, seconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool ExpireAt(string key, DateTime timestamp) => Call("EXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public string[] Keys(string pattern) => Call("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public void Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call("MIGRATE"
            .Input(host, port)
            .Input(key ?? "", destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputIf(keys?.Any() == true, keys)
            .FlagKey(key)
            .FlagKey(keys), rt => rt.ThrowOrValue<string>());
        public bool Move(string key, int db) => Call("MOVE".Input(key, db).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public long? ObjectRefCount(string key) => Call("OBJECT".SubCommand( "REFCOUNT").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long?>());
        public long ObjectIdleTime(string key) => Call("OBJECT".SubCommand("IDLETIME").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public object ObjectEncoding(string key) => Call("OBJECT".SubCommand("ENCODING").Input(key).FlagKey(key), rt => rt.ThrowOrValue<object>());
        public object ObjectFreq(string key) => Call("OBJECT".SubCommand("FREQ").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
        public object ObjectHelp(string key) => Call("OBJECT".SubCommand("HELP").Input(key).FlagKey(key), rt => rt.ThrowOrValue());

        public bool Persist(string key) => Call("PERSIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool PExpire(string key, int milliseconds) => Call("PEXPIRE".Input(key, milliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool PExpireAt(string key, DateTime timestamp) => Call("PEXPIREAT".Input(key, (long)timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long PTtl(string key) => Call("PTTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string RandomKey() => Call("RANDOMKEY", rt => rt.ThrowOrValue<string>());
        public void Rename(string key, string newkey) => Call("RENAME".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue<string>());
        public bool RenameNx(string key, string newkey) => Call("RENAMENX".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue<bool>());
        public void Restore(string key, byte[] serializedValue) => Restore(key, 0, serializedValue);
        public void Restore(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => Call("RESTORE"
            .Input(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public ScanResult<string> Scan(long cursor, string pattern, long count, string type) => Call("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public object Sort(string key, string byPattern, long offset, long count, string[] getPatterns, Collation? collation, bool alpha, string storeDestination) => Call("SORT"
            .Input(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
            .FlagKey(key, storeDestination), rt => rt.ThrowOrValue());

        public long Touch(params string[] keys) => Call("TOUCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public long Ttl(string key) => Call("TTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public KeyType Type(string key) => Call("TYPE".Input(key).FlagKey(key), rt => rt.ThrowOrValue<KeyType>());
        public long UnLink(params string[] keys) => Call("UNLINK".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public long Wait(long numreplicas, long timeoutMilliseconds) => Call("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
    }
}

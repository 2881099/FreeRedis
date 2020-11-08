using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<long> DelAsync(params string[] keys) => CallAsync("DEL".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<byte[]> DumpAsync(string key) => CallAsync("DUMP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());
        public Task<bool> ExistsAsync(string key) => CallAsync("EXISTS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<long> ExistsAsync(string[] keys) => CallAsync("EXISTS".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<bool> ExpireAsync(string key, int seconds) => CallAsync("EXPIRE".Input(key, seconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<bool> ExpireAtAsync(string key, DateTime timestamp) => CallAsync("EXPIREAT".Input(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public Task<string[]> KeysAsync(string pattern) => CallAsync("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public Task MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => CallAsync("MIGRATE"
            .Input(host, port)
            .Input(key ?? "", destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputIf(keys?.Any() == true, keys)
            .FlagKey(key)
            .FlagKey(keys), rt => rt.ThrowOrValue<string>());
        public Task<bool> MoveAsync(string key, int db) => CallAsync("MOVE".Input(key, db).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public Task<long?> ObjectRefCountAsync(string key) => CallAsync("OBJECT".SubCommand("REFCOUNT").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long?>());
        public Task<long> ObjectIdleTimeAsync(string key) => CallAsync("OBJECT".SubCommand("IDLETIME").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<string> ObjectEncodingAsync(string key) => CallAsync("OBJECT".SubCommand("ENCODING").Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<long?> ObjectFreqAsync(string key) => CallAsync("OBJECT".SubCommand("FREQ").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long?>());

        public Task<bool> PersistAsync(string key) => CallAsync("PERSIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<bool> PExpireAsync(string key, int milliseconds) => CallAsync("PEXPIRE".Input(key, milliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<bool> PExpireAtAsync(string key, DateTime timestamp) => CallAsync("PEXPIREAT".Input(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<long> PTtlAsync(string key) => CallAsync("PTTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string> RandomKeyAsync() => CallAsync("RANDOMKEY", rt => rt.ThrowOrValue<string>());
        public Task RenameAsync(string key, string newkey) => CallAsync("RENAME".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue<string>());
        public Task<bool> RenameNxAsync(string key, string newkey) => CallAsync("RENAMENX".Input(key, newkey).FlagKey(key, newkey), rt => rt.ThrowOrValue<bool>());
        public Task RestoreAsync(string key, byte[] serializedValue) => RestoreAsync(key, 0, serializedValue);
        public Task RestoreAsync(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => CallAsync("RESTORE"
            .Input(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public Task<ScanResult<string>> ScanAsync(long cursor, string pattern, long count, string type) => CallAsync("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<string[]> SortAsync(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .Input(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<long> SortStoreAsync(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .Input(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
            .FlagKey(key, storeDestination), rt => rt.ThrowOrValue<long>());

        public Task<long> TouchAsync(params string[] keys) => CallAsync("TOUCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<long> TtlAsync(string key) => CallAsync("TTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<KeyType> TypeAsync(string key) => CallAsync("TYPE".Input(key).FlagKey(key), rt => rt.ThrowOrValue<KeyType>());
        public Task<long> UnLinkAsync(params string[] keys) => CallAsync("UNLINK".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<long> WaitAsync(long numreplicas, long timeoutMilliseconds) => CallAsync("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public long Del(params string[] keys) => Call("DEL".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public byte[] Dump(string key) => Call("DUMP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());
        public bool Exists(string key) => Call("EXISTS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long Exists(string[] keys) => Call("EXISTS".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public bool Expire(string key, int seconds) => Call("EXPIRE".Input(key, seconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool ExpireAt(string key, DateTime timestamp) => Call("EXPIREAT".Input(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());

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
        public string ObjectEncoding(string key) => Call("OBJECT".SubCommand("ENCODING").Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public long? ObjectFreq(string key) => Call("OBJECT".SubCommand("FREQ").Input(key).FlagKey(key), rt => rt.ThrowOrValue<long?>());

        public bool Persist(string key) => Call("PERSIST".Input(key).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool PExpire(string key, int milliseconds) => Call("PEXPIRE".Input(key, milliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool PExpireAt(string key, DateTime timestamp) => Call("PEXPIREAT".Input(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds).FlagKey(key), rt => rt.ThrowOrValue<bool>());
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

        public string[] Sort(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .Input(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public long SortStore(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .Input(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE", storeDestination)
            .FlagKey(key, storeDestination), rt => rt.ThrowOrValue<long>());

        public long Touch(params string[] keys) => Call("TOUCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public long Ttl(string key) => Call("TTL".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public KeyType Type(string key) => Call("TYPE".Input(key).FlagKey(key), rt => rt.ThrowOrValue<KeyType>());
        public long UnLink(params string[] keys) => Call("UNLINK".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public long Wait(long numreplicas, long timeoutMilliseconds) => Call("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
    }
}

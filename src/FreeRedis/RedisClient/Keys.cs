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
        public Task<long> DelAsync(params string[] keys) => CallAsync("DEL".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<byte[]> DumpAsync(string key) => CallAsync("DUMP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());
        public Task<bool> ExistsAsync(string key) => CallAsync("EXISTS".InputKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<long> ExistsAsync(string[] keys) => CallAsync("EXISTS".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<bool> ExpireAsync(string key, int seconds) => CallAsync("EXPIRE".InputKey(key, seconds), rt => rt.ThrowOrValue<bool>());
        public Task<bool> ExpireAtAsync(string key, DateTime timestamp) => CallAsync("EXPIREAT".InputKey(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds), rt => rt.ThrowOrValue<bool>());

        public Task<string[]> KeysAsync(string pattern) => CallAsync("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public Task MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => CallAsync("MIGRATE"
            .Input(host, port)
            .InputKey(key ?? "")
            .Input(destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputKey(keys), rt => rt.ThrowOrValue<string>());
        public Task<bool> MoveAsync(string key, int db) => CallAsync("MOVE".InputKey(key, db), rt => rt.ThrowOrValue<bool>());

        public Task<long?> ObjectRefCountAsync(string key) => CallAsync("OBJECT".SubCommand("REFCOUNT").InputKey(key), rt => rt.ThrowOrValue<long?>());
        public Task<long> ObjectIdleTimeAsync(string key) => CallAsync("OBJECT".SubCommand("IDLETIME").InputKey(key), rt => rt.ThrowOrValue<long>());
        public Task<string> ObjectEncodingAsync(string key) => CallAsync("OBJECT".SubCommand("ENCODING").InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<long?> ObjectFreqAsync(string key) => CallAsync("OBJECT".SubCommand("FREQ").InputKey(key), rt => rt.ThrowOrValue<long?>());

        public Task<bool> PersistAsync(string key) => CallAsync("PERSIST".InputKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<bool> PExpireAsync(string key, int milliseconds) => CallAsync("PEXPIRE".InputKey(key, milliseconds), rt => rt.ThrowOrValue<bool>());
        public Task<bool> PExpireAtAsync(string key, DateTime timestamp) => CallAsync("PEXPIREAT".InputKey(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds), rt => rt.ThrowOrValue<bool>());
        public Task<long> PTtlAsync(string key) => CallAsync("PTTL".InputKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string> RandomKeyAsync() => CallAsync("RANDOMKEY", rt => rt.ThrowOrValue<string>());
        public Task RenameAsync(string key, string newkey) => CallAsync("RENAME".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<string>());
        public Task<bool> RenameNxAsync(string key, string newkey) => CallAsync("RENAMENX".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<bool>());
        public Task RestoreAsync(string key, byte[] serializedValue) => RestoreAsync(key, 0, serializedValue);
        public Task RestoreAsync(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => CallAsync("RESTORE"
            .InputKey(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency), rt => rt.ThrowOrValue<string>());

        public Task<ScanResult<string>> ScanAsync(long cursor, string pattern, long count, string type) => CallAsync("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<string[]> SortAsync(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA"), rt => rt.ThrowOrValue<string[]>());
        public Task<long> SortStoreAsync(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE")
            .InputKeyIf(!string.IsNullOrWhiteSpace(storeDestination), storeDestination), rt => rt.ThrowOrValue<long>());

        public Task<long> TouchAsync(params string[] keys) => CallAsync("TOUCH".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<long> TtlAsync(string key) => CallAsync("TTL".InputKey(key), rt => rt.ThrowOrValue<long>());
        public Task<KeyType> TypeAsync(string key) => CallAsync("TYPE".InputKey(key), rt => rt.ThrowOrValue<KeyType>());
        public Task<long> UnLinkAsync(params string[] keys) => CallAsync("UNLINK".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<long> WaitAsync(long numreplicas, long timeoutMilliseconds) => CallAsync("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public long Del(params string[] keys) => Call("DEL".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public byte[] Dump(string key) => Call("DUMP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());
        public bool Exists(string key) => Call("EXISTS".InputKey(key), rt => rt.ThrowOrValue<bool>());
        public long Exists(string[] keys) => Call("EXISTS".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public bool Expire(string key, int seconds) => Call("EXPIRE".InputKey(key, seconds), rt => rt.ThrowOrValue<bool>());
        public bool ExpireAt(string key, DateTime timestamp) => Call("EXPIREAT".InputKey(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds), rt => rt.ThrowOrValue<bool>());

        public string[] Keys(string pattern) => Call("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public void Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call("MIGRATE"
            .Input(host, port)
            .InputKey(key ?? "")
            .Input(destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputKey(keys), rt => rt.ThrowOrValue<string>());
        public bool Move(string key, int db) => Call("MOVE".InputKey(key, db), rt => rt.ThrowOrValue<bool>());

        public long? ObjectRefCount(string key) => Call("OBJECT".SubCommand( "REFCOUNT").InputKey(key), rt => rt.ThrowOrValue<long?>());
        public long ObjectIdleTime(string key) => Call("OBJECT".SubCommand("IDLETIME").InputKey(key), rt => rt.ThrowOrValue<long>());
        public string ObjectEncoding(string key) => Call("OBJECT".SubCommand("ENCODING").InputKey(key), rt => rt.ThrowOrValue<string>());
        public long? ObjectFreq(string key) => Call("OBJECT".SubCommand("FREQ").InputKey(key), rt => rt.ThrowOrValue<long?>());

        public bool Persist(string key) => Call("PERSIST".InputKey(key), rt => rt.ThrowOrValue<bool>());
        public bool PExpire(string key, int milliseconds) => Call("PEXPIRE".InputKey(key, milliseconds), rt => rt.ThrowOrValue<bool>());
        public bool PExpireAt(string key, DateTime timestamp) => Call("PEXPIREAT".InputKey(key, (long)timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds), rt => rt.ThrowOrValue<bool>());
        public long PTtl(string key) => Call("PTTL".InputKey(key), rt => rt.ThrowOrValue<long>());

        public string RandomKey() => Call("RANDOMKEY", rt => rt.ThrowOrValue<string>());
        public void Rename(string key, string newkey) => Call("RENAME".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<string>());
        public bool RenameNx(string key, string newkey) => Call("RENAMENX".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<bool>());
        public void Restore(string key, byte[] serializedValue) => Restore(key, 0, serializedValue);
        public void Restore(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => Call("RESTORE"
            .InputKey(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency), rt => rt.ThrowOrValue<string>());

        public ScanResult<string> Scan(long cursor, string pattern, long count, string type) => Call("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public string[] Sort(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA"), rt => rt.ThrowOrValue<string[]>());
        public long SortStore(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] { "GET", a }).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE")
            .InputKeyIf(!string.IsNullOrWhiteSpace(storeDestination), storeDestination), rt => rt.ThrowOrValue<long>());

        public long Touch(params string[] keys) => Call("TOUCH".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public long Ttl(string key) => Call("TTL".InputKey(key), rt => rt.ThrowOrValue<long>());
        public KeyType Type(string key) => Call("TYPE".InputKey(key), rt => rt.ThrowOrValue<KeyType>());
        public long UnLink(params string[] keys) => Call("UNLINK".InputKey(keys), rt => rt.ThrowOrValue<long>());
        public long Wait(long numreplicas, long timeoutMilliseconds) => Call("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
    }
}

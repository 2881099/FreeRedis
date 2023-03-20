using FreeRedis.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)

        /// <summary>
        /// DEL command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Removes the specified keys. A key is ignored if it does not exist.<br /><br />
        /// <br />
        /// 移除指定的键。如果键不存在，则忽略之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/del <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were removed.</returns>
        public Task<long> DelAsync(params string[] keys) => CallAsync("DEL".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DUMP command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Serialize the value stored at key in a Redis-specific format and return it to the user. The returned value can be synthesized back into a Redis key using the RESTORE command.<br /><br />
        /// <br />
        /// 以 Redis 特有格式序列化指定的键值，并返回给用户。可结合 RESTORE 命令将序列化的结果重新写回 Redis。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/dump <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The serialized value.</returns>
        public Task<byte[]> DumpAsync(string key) => CallAsync("DUMP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());

        /// <summary>
        /// EXISTS command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns if key exists.<br /><br />
        /// <br />
        /// 返回键是否存在。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/exists <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Specifically: True if the key exists. False if the key does not exist.</returns>
        public Task<bool> ExistsAsync(string key) => CallAsync("EXISTS".InputKey(key), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// EXISTS command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns if key exists.<br /><br />
        /// <br />
        /// 返回键是否存在。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/exists <br />
        /// Available since 3.0.3. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys existing among the ones specified as arguments. Keys mentioned multiple times and existing are counted multiple times.</returns>
        public Task<long> ExistsAsync(string[] keys) => CallAsync("EXISTS".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// EXPIRE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set a timeout on key. After the timeout has expired, the key will automatically be deleted. A key with an associated timeout is often said to be volatile in Redis terminology.<br /><br />
        /// <br />
        /// 设置键的超时时间。到期后，键将被自动删除。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expire <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="seconds">Expires time (seconds)</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public Task<bool> ExpireAsync(string key, int seconds) => CallAsync("EXPIRE".InputKey(key, seconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// EXPIREAT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// EXPIREAT has the same effect and semantic as EXPIRE, but instead of specifying the number of seconds representing the TTL (time to live), it takes an absolute Unix timestamp (seconds since January 1, 1970). A timestamp in the past will delete the key immediately.<br /><br />
        /// <br />
        /// EXPIREAT 具有与 EXPIRE 相同的作用和语义，但是它没有指定表示 TTL（生存时间）的秒数，而是使用了绝对的 Unix 时间戳（自1970年1月1日以来的秒数）。时间戳一旦过期就会被删除。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expireat <br />
        /// Available since 1.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timestamp">UNIX Timestamp</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public Task<bool> ExpireAtAsync(string key, DateTime timestamp) => CallAsync("EXPIREAT".InputKey(key, (long) timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// KEYS command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns all keys matching pattern.<br /><br />
        /// <br />
        /// 返回所有与模式匹配的键。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/keys <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="pattern">Pattern</param>
        /// <returns>List of keys matching pattern.</returns>
        public Task<string[]> KeysAsync(string pattern) => CallAsync("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// MIGRATE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Atomically transfer a key from a source Redis instance to a destination Redis instance. On success the key is deleted from the original instance and is guaranteed to exist in the target instance.<br /><br />
        /// <br />
        /// 原子地将键从 Redis 源实例转移到目标实例。转移成功后，Key 将从源实例中删除，并确保保存在目标实例之中。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/migrate <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="host">Destination Instance's Host</param>
        /// <param name="port">Destination Instance's Port</param>
        /// <param name="key">Key</param>
        /// <param name="destinationDb">Destination Instance's Database</param>
        /// <param name="timeoutMilliseconds">Timeout milliseconds</param>
        /// <param name="copy">Do not remove the key from the local instance. Available since 3.0.0. </param>
        /// <param name="replace">Replace existing key on the remote instance. Available since 3.0.0. </param>
        /// <param name="authPassword">Authenticate with the given password to the remote instance. Available since 4.0.7. </param>
        /// <param name="auth2Username">Authenticate with the given username (Redis 6 or greater ACL auth style).</param>
        /// <param name="auth2Password">Authenticate with the given password (Redis 6 or greater ACL auth style).</param>
        /// <param name="keys">If the key argument is an empty string, the command will instead migrate all the keys that follow the KEYS option (see the above section for more info). Available since 3.0.6. </param>
        public Task MigrateAsync(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => CallAsync("MIGRATE"
            .Input(host, port)
            .InputKey(key ?? "")
            .Input(destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputKey(keys), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// MOVE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Move key from the currently selected database (see SELECT) to the specified destination database. When key already exists in the destination database, or it does not exist in the source database, it does nothing. It is possible to use MOVE as a locking primitive because of this. <br /><br />
        /// <br />
        /// 将指定的 Key 从当前数据库移动到目标数据库。<br />
        /// 如果目标数据库中已存在该键，或源数据库中不存在该键，则什么都不做。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/move <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="db">Database</param>
        /// <returns>Specifically: True if key was moved. False if key was not moved.</returns>
        public Task<bool> MoveAsync(string key, int db) => CallAsync("MOVE".InputKey(key, db), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// OBJECT REFCOUNT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the number of references of the value associated with the specified key. This command is mainly useful for debugging.<br /><br />
        /// <br />
        /// 返回与指定键关联的值的引用数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the number of references of the value associated with the specified key.</returns>
        public Task<long?> ObjectRefCountAsync(string key) => CallAsync("OBJECT".SubCommand("REFCOUNT").InputKey(key), rt => rt.ThrowOrValue<long?>());

        /// <summary>
        /// OBJECT IDLETIME command (An Asynchronous Version) <br /><br />
        /// <br />
        /// returns the number of seconds since the object stored at the specified key is idle (not requested by read or write operations). While the value is returned in seconds the actual resolution of this timer is 10 seconds, but may vary in future implementations. This subcommand is available when maxmemory-policy is set to an LRU policy or noeviction and maxmemory is set. <br /><br />
        /// <br />
        /// 返回指定键值的空闲时间，单位为秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the number of seconds since the object stored at the specified key is idle</returns>
        public Task<long> ObjectIdleTimeAsync(string key) => CallAsync("OBJECT".SubCommand("IDLETIME").InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// OBJECT ENCODING command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the kind of internal representation used in order to store the value associated with a key.<br /><br />
        /// <br />
        /// 返回用于存储与键关联的值的内部表示形式的类型。
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the kind of internal representation used in order to store the value associated with a key.</returns>
        public Task<string> ObjectEncodingAsync(string key) => CallAsync("OBJECT".SubCommand("ENCODING").InputKey(key), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// OBJECT FREQ command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the logarithmic access frequency counter of the object stored at the specified key. This subcommand is available when maxmemory-policy is set to an LFU policy.<br /><br />
        /// <br />
        /// 返回存储在指定键处的对象的对数访问频率计数器。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the logarithmic access frequency counter of the object stored at the specified key.</returns>
        public Task<long?> ObjectFreqAsync(string key) => CallAsync("OBJECT".SubCommand("FREQ").InputKey(key), rt => rt.ThrowOrValue<long?>());

        /// <summary>
        /// PERSIST command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Remove the existing timeout on key, turning the key from volatile (a key with an expire set) to persistent (a key that will never expire as no timeout is associated).<br /><br />
        /// <br />
        /// 将指定键的超时设置移除，将键从可变键转换为持久键。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/persist <br />
        /// Available since 2.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Specifically: True if the timeout was removed. False if key does not exist or does not have an associated timeout.</returns>
        public Task<bool> PersistAsync(string key) => CallAsync("PERSIST".InputKey(key), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PEXPIRE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// This command works exactly like EXPIRE but the time to live of the key is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// 此命令与 EXPIRE 相同，区别仅仅在于时间单位是毫秒，不是秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expire <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="milliseconds">Expires time (milliseconds)</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public Task<bool> PExpireAsync(string key, int milliseconds) => CallAsync("PEXPIRE".InputKey(key, milliseconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PEXPIREAT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// PEXPIREAT has the same effect and semantic as EXPIREAT, but the Unix time at which the key will expire is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// PEXPIREAT 具有与 EXPIREAT 相同的作用和语义，但 Key 的时间戳使用的是毫秒，而不是秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/pexpireat <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timestamp">UNIX Timestamp</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        /// <returns></returns>
        public Task<bool> PExpireAtAsync(string key, DateTime timestamp) => CallAsync("PEXPIREAT".InputKey(key, (long) timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PTTL command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Like TTL this command returns the remaining time to live of a key that has an expire set, with the sole difference that TTL returns the amount of remaining time in seconds while PTTL returns it in milliseconds.<br />
        /// In Redis 2.6 or older the command returns -1 if the key does not exist or if the key exist but has no associated expire. <br />
        /// Starting with Redis 2.8 the return value in case of error changed: <br />
        ///  - The command returns -2 if the key does not exist.<br />
        ///  - The command returns -1 if the key exists but has no associated expire.<br /><br />
        /// <br />
        /// 与 TTL 命令一样，返回键的剩余生存时间，唯一的区别是 TTL 以秒为单位返回剩余时间，而 PTTL 以毫秒为单位返回。<br />
        /// 在 Redis 2.6 之前，如果 Key 不存在或 Key 未设置过期时间，则返回 -1<br />
        /// 从 Redis 2.8 开始，将针对不同的错误情况返回不同的值：<br />
        ///  - 如果 Key 不存在，则返回 -2 <br />
        ///  - 如果 Key 存在，但没有设置过过期时间，则返回 -1 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/pttl <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>TTL in milliseconds, or a negative value in order to signal an error (see the description above).</returns>
        public Task<long> PTtlAsync(string key) => CallAsync("PTTL".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// RANDOMKEY command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Return a random key from the currently selected database. <br /><br />
        /// <br />
        /// 从当前选择的数据库返回一个随机密钥。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/randomkey <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <returns>The random key, or nil when the database is empty.</returns>
        public Task<string> RandomKeyAsync() => CallAsync("RANDOMKEY", rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// RENAME command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Renames key to newkey. It returns an error when key does not exist. If newkey already exists it is overwritten, when this happens RENAME executes an implicit DEL operation, so if the deleted key contains a very big value it may cause high latency even if RENAME itself is usually a constant-time operation.<br />
        ///  - Before Redis 3.2.0, an error is returned if source and destination names are the same.<br /><br />
        /// <br />
        /// 将 Key 重命名为 New Key。如果键不存在，返回错误。<br />
        /// 如果 New Key 已存在，则将被覆盖，此时类似 DEL 命令，由于 RENAME 是 constant-time operation，因此当删除的键有很大的值时会有较大的延迟。<br />
        ///  - 在 Redis 3.2 之前，如果 Key 和 New Key 名字一样，将返回错误。<br /><br /> 
        /// <br />
        /// Document link: https://redis.io/commands/rename <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newkey">New key</param>
        public Task RenameAsync(string key, string newkey) => CallAsync("RENAME".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// RENAMENX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Renames key to newkey. It returns an error when key does not exist. If newkey already exists it is overwritten, when this happens RENAME executes an implicit DEL operation, so if the deleted key contains a very big value it may cause high latency even if RENAME itself is usually a constant-time operation.<br />
        ///  - Before Redis 3.2.0, an error is returned if source and destination names are the same.<br /><br />
        /// <br />
        /// 将 Key 重命名为 New Key。如果键不存在，返回错误。<br />
        /// 如果 New Key 已存在，则将被覆盖，此时类似 DEL 命令，由于 RENAME 是 constant-time operation，因此当删除的键有很大的值时会有较大的延迟。<br />
        ///  - 在 Redis 3.2 之前，如果 Key 和 New Key 名字一样，将返回错误。<br /><br /> 
        /// <br />
        /// Document link: https://redis.io/commands/renamenx <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newkey">New key</param>
        /// <returns>specifically: True if key was renamed to newkey. False if newkey already exists.</returns>
        public Task<bool> RenameNxAsync(string key, string newkey) => CallAsync("RENAMENX".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// RESTORE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Create a key associated with a value that is obtained by deserializing the provided serialized value (obtained via DUMP).<br /><br />
        /// <br />
        /// 将经由 DUMP 命令序列化的值反序列化后，作为给定键的值进行保存。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/restore <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="serializedValue">Serialized value</param>
        public Task RestoreAsync(string key, byte[] serializedValue) => RestoreAsync(key, 0, serializedValue);

        /// <summary>
        /// RESTORE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Create a key associated with a value that is obtained by deserializing the provided serialized value (obtained via DUMP).<br /><br />
        /// <br />
        /// 将经由 DUMP 命令序列化的值反序列化后，作为给定键的值进行保存。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/restore <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="ttl">If ttl is 0 the key is created without any expire, otherwise the specified expire time (in milliseconds) is set.</param>
        /// <param name="serializedValue">Serialized value</param>
        /// <param name="replace">REPLACE modifier: If the Key already exists, replace it. Available since 3.0.0.</param>
        /// <param name="absTtl">Absolute Unix timestamp (in milliseconds) in which the key will expire. Available since 5.0.0.</param>
        /// <param name="idleTimeSeconds">IDLETIME modifier. Available since 5.0.0.</param>
        /// <param name="frequency">FREQ modifier. Available since 5.0.0.</param>
        public Task RestoreAsync(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => CallAsync("RESTORE"
            .InputKey(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// SCAN command (An Asynchronous Version) <br /><br />
        /// <br />
        /// The SCAN command and the closely related commands SSCAN, HSCAN and ZSCAN are used in order to incrementally iterate over a collection of elements.<br /><br />
        /// <br />
        /// 使用 SCAN 命令及其关联的 SSCAN、HSCAN、ZSCAN 等命令来迭代返回元素集合。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/scan <br />
        /// Available since 2.8.0. 
        /// </summary>
        /// <param name="cursor">Cursor</param>
        /// <param name="pattern">MATCH option</param>
        /// <param name="count">COUNT option: while SCAN does not provide guarantees about the number of elements returned at every iteration, it is possible to empirically adjust the behavior of SCAN using the COUNT option, default is 10</param>
        /// <param name="type">TYPE option: the type argument is the same string name that the TYPE command returns. Available since 6.0</param>
        /// <returns>Return a two elements multi-bulk reply, where the first element is a string representing an unsigned 64 bit number (the cursor), and the second element is a multi-bulk with an array of elements.</returns>
        public Task<ScanResult<string>> ScanAsync(long cursor, string pattern, long count, string type) => CallAsync("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        /// <summary>
        /// SORT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns or stores the elements contained in the list, set or sorted set at key. By default, sorting is numeric and elements are compared by their value interpreted as double precision floating point number. <br /><br />
        /// <br />
        /// 返回含在 LIST、SET、ZSET 中的元素。默认情况下，排序是数字形式的，并且将元素的值进行比较以解释为双精度浮点数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/sort <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Keys</param>
        /// <param name="byPattern">BY modifier</param>
        /// <param name="offset">The number of elements to skip.</param>
        /// <param name="count">Specifying the number of elements to return from starting at offset.</param>
        /// <param name="getPatterns">GET modifier</param>
        /// <param name="collation">ASC | DESC modifier</param>
        /// <param name="alpha">ALPHA modifier, sort by lexicographically.</param>
        /// <returns>A list of sorted elements</returns>
        public Task<string[]> SortAsync(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] {"GET", a}).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA"), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// SORT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Stores the elements contained in the list, set or sorted set at key. By default, sorting is numeric and elements are compared by their value interpreted as double precision floating point number. <br /><br />
        /// <br />
        /// 存储包含在 LIST、SET、ZSET 中的元素。默认情况下，排序是数字形式的，并且将元素的值进行比较以解释为双精度浮点数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/sort <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="storeDestination">Storing the result of a SORT operation to the destination key.</param>
        /// <param name="key">Keys</param>
        /// <param name="byPattern">BY modifier</param>
        /// <param name="offset">The number of elements to skip.</param>
        /// <param name="count">Specifying the number of elements to return from starting at offset.</param>
        /// <param name="getPatterns">GET modifier</param>
        /// <param name="collation">ASC | DESC modifier</param>
        /// <param name="alpha">ALPHA modifier, sort by lexicographically.</param>
        /// <returns>The number of sorted elements in the destination list.</returns>
        public Task<long> SortStoreAsync(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => CallAsync("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] {"GET", a}).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE")
            .InputKeyIf(!string.IsNullOrWhiteSpace(storeDestination), storeDestination), rt => rt.ThrowOrValue<long>());
        
        /// <summary>
        /// TOUCH command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Alters the last access time of a key(s). A key is ignored if it does not exist.<br /><br />
        /// <br />
        /// 更改 Key(s) 最后访问时间。如果键不存在，则忽略之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/touch <br />
        /// Available since 3.2.1. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were touched.</returns>
        public Task<long> TouchAsync(params string[] keys) => CallAsync("TOUCH".InputKey(keys), rt => rt.ThrowOrValue<long>());
       
        /// <summary>
        /// TTL command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the remaining time to live of a key that has a timeout. This introspection capability allows a Redis client to check how many seconds a given key will continue to be part of the dataset.<br />
        /// In Redis 2.6 or older the command returns -1 if the key does not exist or if the key exist but has no associated expire. <br />
        /// Starting with Redis 2.8 the return value in case of error changed: <br />
        ///  - The command returns -2 if the key does not exist.<br />
        ///  - The command returns -1 if the key exists but has no associated expire.<br /><br />
        /// <br />
        /// 返回键的剩余生存时间。这种自省能力允许 Redis 客户端检查指定键还能再数据集中生存多少秒。<br />
        /// 在 Redis 2.6 之前，如果 Key 不存在或 Key 未设置过期时间，则返回 -1<br />
        /// 从 Redis 2.8 开始，将针对不同的错误情况返回不同的值：<br />
        ///  - 如果 Key 不存在，则返回 -2 <br />
        ///  - 如果 Key 存在，但没有设置过过期时间，则返回 -1 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/ttl <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>TTL in seconds, or a negative value in order to signal an error (see the description above).</returns>
        public Task<long> TtlAsync(string key) => CallAsync("TTL".InputKey(key), rt => rt.ThrowOrValue<long>());
       
        /// <summary>
        /// TYPE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the string representation of the type of the value stored at key. The different types that can be returned are: string, list, set, zset, hash and stream.<br /><br />
        /// <br />
        /// 获取键对应值的类型的字符串表达形式。可以返回的类型是：string，list，set，zset，hash 以及 stream。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/type <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The type of key, or none when key does not exist.</returns>
        public Task<KeyType> TypeAsync(string key) => CallAsync("TYPE".InputKey(key), rt => rt.ThrowOrValue<KeyType>());
      
        /// <summary>
        /// UNLINK command (An Asynchronous Version) <br /><br />
        /// <br />
        /// This command is very similar to DEL: it removes the specified keys. Just like DEL a key is ignored if it does not exist. However the command performs the actual memory reclaiming in a different thread, so it is not blocking, while DEL is. <br /><br />
        /// <br />
        /// 本命令与 DEL 相似，能删除指定的键值；如果键不存在，则忽略。与 DEL 不同的是，本命令将在另一个线程中执行实际的内存回收，因此是非阻塞的。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/unlink <br />
        /// Available since 4.0.0. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were unlinked.</returns>
        public Task<long> UnLinkAsync(params string[] keys) => CallAsync("UNLINK".InputKey(keys), rt => rt.ThrowOrValue<long>());
       
        /// <summary>
        /// WAIT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// This command blocks the current client until all the previous write commands are successfully transferred and acknowledged by at least the specified number of replicas. If the timeout, specified in milliseconds, is reached, the command returns even if the specified number of replicas were not yet reached. <br /><br />
        /// <br />
        /// 本命令将阻塞当前客户端，直到所有写命令成功发送、且大于等于指定数量的副本进行了确认。<br />
        /// 如果超时（单位为毫秒），即便没能获得指定数量副本的确认，命令也会返回。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/wait <br />
        /// Available since 3.0.0. 
        /// </summary>
        /// <param name="numreplicas">The number of replicas</param>
        /// <param name="timeoutMilliseconds">Timeout milliseconds</param>
        /// <returns>The command returns the number of replicas reached by all the writes performed in the context of the current connection.</returns>
        public Task<long> WaitAsync(long numreplicas, long timeoutMilliseconds) => CallAsync("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());

        #endregion

#endif

        /// <summary>
        /// DEL command (A Synchronized Version) <br /><br />
        /// <br />
        /// Removes the specified keys. A key is ignored if it does not exist.<br /><br />
        /// <br />
        /// 移除指定的键。如果键不存在，则忽略之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/del <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were removed.</returns>
        public long Del(params string[] keys) => Call("DEL".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DUMP command (A Synchronized Version) <br /><br />
        /// <br />
        /// Serialize the value stored at key in a Redis-specific format and return it to the user. The returned value can be synthesized back into a Redis key using the RESTORE command.<br /><br />
        /// <br />
        /// 以 Redis 特有格式序列化指定的键值，并返回给用户。可结合 RESTORE 命令将序列化的结果重新写回 Redis。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/dump <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The serialized value.</returns>
        public byte[] Dump(string key) => Call("DUMP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue<byte[]>());

        /// <summary>
        /// EXISTS command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns if key exists.<br /><br />
        /// <br />
        /// 返回键是否存在。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/exists <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Specifically: True if the key exists. False if the key does not exist.</returns>
        public bool Exists(string key) => Call("EXISTS".InputKey(key), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// EXISTS command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns if key exists.<br /><br />
        /// <br />
        /// 返回键是否存在。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/exists <br />
        /// Available since 3.0.3. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys existing among the ones specified as arguments. Keys mentioned multiple times and existing are counted multiple times.</returns>
        public long Exists(string[] keys) => Call("EXISTS".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// EXPIRE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set a timeout on key. After the timeout has expired, the key will automatically be deleted. A key with an associated timeout is often said to be volatile in Redis terminology.<br /><br />
        /// <br />
        /// 设置键的超时时间。到期后，键将被自动删除。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expire <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="seconds">Expires time (seconds)</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public bool Expire(string key, int seconds) => Call("EXPIRE".InputKey(key, seconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// EXPIRE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set a timeout on key. After the timeout has expired, the key will automatically be deleted. A key with an associated timeout is often said to be volatile in Redis terminology.<br /><br />
        /// <br />
        /// 设置键的超时时间。到期后，键将被自动删除。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expire <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="expire">Expires TimeSpan</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public bool Expire(string key, TimeSpan expire) => Call("EXPIRE".InputKey(key, (long)expire.TotalSeconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// EXPIREAT command (A Synchronized Version) <br /><br />
        /// <br />
        /// EXPIREAT has the same effect and semantic as EXPIRE, but instead of specifying the number of seconds representing the TTL (time to live), it takes an absolute Unix timestamp (seconds since January 1, 1970). A timestamp in the past will delete the key immediately.<br /><br />
        /// <br />
        /// EXPIREAT 具有与 EXPIRE 相同的作用和语义，但是它没有指定表示 TTL（生存时间）的秒数，而是使用了绝对的 Unix 时间戳（自1970年1月1日以来的秒数）。时间戳一旦过期就会被删除。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expireat <br />
        /// Available since 1.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timestamp">UNIX Timestamp</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public bool ExpireAt(string key, DateTime timestamp) => Call("EXPIREAT".InputKey(key, (long) timestamp.ToUniversalTime().Subtract(_epoch).TotalSeconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// KEYS command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns all keys matching pattern.<br /><br />
        /// <br />
        /// 返回所有与模式匹配的键。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/keys <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="pattern">Pattern</param>
        /// <returns>List of keys matching pattern.</returns>
        public string[] Keys(string pattern) => Call("KEYS".Input(pattern), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// MIGRATE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Atomically transfer a key from a source Redis instance to a destination Redis instance. On success the key is deleted from the original instance and is guaranteed to exist in the target instance.<br /><br />
        /// <br />
        /// 原子地将键从 Redis 源实例转移到目标实例。转移成功后，Key 将从源实例中删除，并确保保存在目标实例之中。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/migrate <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="host">Destination Instance's Host</param>
        /// <param name="port">Destination Instance's Port</param>
        /// <param name="key">Key</param>
        /// <param name="destinationDb">Destination Instance's Database</param>
        /// <param name="timeoutMilliseconds">Timeout milliseconds</param>
        /// <param name="copy">Do not remove the key from the local instance. Available since 3.0.0. </param>
        /// <param name="replace">Replace existing key on the remote instance. Available since 3.0.0. </param>
        /// <param name="authPassword">Authenticate with the given password to the remote instance. Available since 4.0.7. </param>
        /// <param name="auth2Username">Authenticate with the given username (Redis 6 or greater ACL auth style).</param>
        /// <param name="auth2Password">Authenticate with the given password (Redis 6 or greater ACL auth style).</param>
        /// <param name="keys">If the key argument is an empty string, the command will instead migrate all the keys that follow the KEYS option (see the above section for more info). Available since 3.0.6. </param>
        public void Migrate(string host, int port, string key, int destinationDb, long timeoutMilliseconds, bool copy, bool replace, string authPassword, string auth2Username, string auth2Password, string[] keys) => Call("MIGRATE"
            .Input(host, port)
            .InputKey(key ?? "")
            .Input(destinationDb, timeoutMilliseconds)
            .InputIf(copy, "COPY")
            .InputIf(replace, "REPLACE")
            .InputIf(!string.IsNullOrWhiteSpace(authPassword), "AUTH", authPassword)
            .InputIf(!string.IsNullOrWhiteSpace(auth2Username) && !string.IsNullOrWhiteSpace(auth2Password), "AUTH2", auth2Username, auth2Password)
            .InputKey(keys), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// MOVE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Move key from the currently selected database (see SELECT) to the specified destination database. When key already exists in the destination database, or it does not exist in the source database, it does nothing. It is possible to use MOVE as a locking primitive because of this. <br /><br />
        /// <br />
        /// 将指定的 Key 从当前数据库移动到目标数据库。<br />
        /// 如果目标数据库中已存在该键，或源数据库中不存在该键，则什么都不做。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/move <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="db">Database</param>
        /// <returns>Specifically: True if key was moved. False if key was not moved.</returns>
        public bool Move(string key, int db) => Call("MOVE".InputKey(key, db), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// OBJECT REFCOUNT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the number of references of the value associated with the specified key. This command is mainly useful for debugging.<br /><br />
        /// <br />
        /// 返回与指定键关联的值的引用数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the number of references of the value associated with the specified key.</returns>
        public long? ObjectRefCount(string key) => Call("OBJECT".SubCommand("REFCOUNT").InputKey(key), rt => rt.ThrowOrValue<long?>());

        /// <summary>
        /// OBJECT IDLETIME command (A Synchronized Version) <br /><br />
        /// <br />
        /// returns the number of seconds since the object stored at the specified key is idle (not requested by read or write operations). While the value is returned in seconds the actual resolution of this timer is 10 seconds, but may vary in future implementations. This subcommand is available when maxmemory-policy is set to an LRU policy or noeviction and maxmemory is set. <br /><br />
        /// <br />
        /// 返回指定键值的空闲时间，单位为秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the number of seconds since the object stored at the specified key is idle</returns>
        public long ObjectIdleTime(string key) => Call("OBJECT".SubCommand("IDLETIME").InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// OBJECT ENCODING command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the kind of internal representation used in order to store the value associated with a key.<br /><br />
        /// <br />
        /// 返回用于存储与键关联的值的内部表示形式的类型。
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the kind of internal representation used in order to store the value associated with a key.</returns>
        public string ObjectEncoding(string key) => Call("OBJECT".SubCommand("ENCODING").InputKey(key), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// OBJECT FREQ command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the logarithmic access frequency counter of the object stored at the specified key. This subcommand is available when maxmemory-policy is set to an LFU policy.<br /><br />
        /// <br />
        /// 返回存储在指定键处的对象的对数访问频率计数器。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/object <br />
        /// Available since 2.2.3. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Returns the logarithmic access frequency counter of the object stored at the specified key.</returns>
        public long? ObjectFreq(string key) => Call("OBJECT".SubCommand("FREQ").InputKey(key), rt => rt.ThrowOrValue<long?>());

        /// <summary>
        /// PERSIST command (A Synchronized Version) <br /><br />
        /// <br />
        /// Remove the existing timeout on key, turning the key from volatile (a key with an expire set) to persistent (a key that will never expire as no timeout is associated).<br /><br />
        /// <br />
        /// 将指定键的超时设置移除，将键从可变键转换为持久键。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/persist <br />
        /// Available since 2.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Specifically: True if the timeout was removed. False if key does not exist or does not have an associated timeout.</returns>
        public bool Persist(string key) => Call("PERSIST".InputKey(key), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PEXPIRE command (A Synchronized Version) <br /><br />
        /// <br />
        /// This command works exactly like EXPIRE but the time to live of the key is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// 此命令与 EXPIRE 相同，区别仅仅在于时间单位是毫秒，不是秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/expire <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="milliseconds">Expires time (milliseconds)</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        public bool PExpire(string key, int milliseconds) => Call("PEXPIRE".InputKey(key, milliseconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PEXPIREAT command (A Synchronized Version) <br /><br />
        /// <br />
        /// PEXPIREAT has the same effect and semantic as EXPIREAT, but the Unix time at which the key will expire is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// PEXPIREAT 具有与 EXPIREAT 相同的作用和语义，但 Key 的时间戳使用的是毫秒，而不是秒。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/pexpireat <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timestamp">UNIX Timestamp</param>
        /// <returns>Specifically: True if the timeout was set.False if key does not exist.</returns>
        /// <returns></returns>
        public bool PExpireAt(string key, DateTime timestamp) => Call("PEXPIREAT".InputKey(key, (long) timestamp.ToUniversalTime().Subtract(_epoch).TotalMilliseconds), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// PTTL command (A Synchronized Version) <br /><br />
        /// <br />
        /// Like TTL this command returns the remaining time to live of a key that has an expire set, with the sole difference that TTL returns the amount of remaining time in seconds while PTTL returns it in milliseconds.<br />
        /// In Redis 2.6 or older the command returns -1 if the key does not exist or if the key exist but has no associated expire. <br />
        /// Starting with Redis 2.8 the return value in case of error changed: <br />
        ///  - The command returns -2 if the key does not exist.<br />
        ///  - The command returns -1 if the key exists but has no associated expire.<br /><br />
        /// <br />
        /// 与 TTL 命令一样，返回键的剩余生存时间，唯一的区别是 TTL 以秒为单位返回剩余时间，而 PTTL 以毫秒为单位返回。<br />
        /// 在 Redis 2.6 之前，如果 Key 不存在或 Key 未设置过期时间，则返回 -1<br />
        /// 从 Redis 2.8 开始，将针对不同的错误情况返回不同的值：<br />
        ///  - 如果 Key 不存在，则返回 -2 <br />
        ///  - 如果 Key 存在，但没有设置过过期时间，则返回 -1 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/pttl <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>TTL in milliseconds, or a negative value in order to signal an error (see the description above).</returns>
        public long PTtl(string key) => Call("PTTL".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// RANDOMKEY command (A Synchronized Version) <br /><br />
        /// <br />
        /// Return a random key from the currently selected database. <br /><br />
        /// <br />
        /// 从当前选择的数据库返回一个随机密钥。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/randomkey <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <returns>The random key, or nil when the database is empty.</returns>
        public string RandomKey() => Call("RANDOMKEY", rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// RENAME command (A Synchronized Version) <br /><br />
        /// <br />
        /// Renames key to newkey. It returns an error when key does not exist. If newkey already exists it is overwritten, when this happens RENAME executes an implicit DEL operation, so if the deleted key contains a very big value it may cause high latency even if RENAME itself is usually a constant-time operation.<br />
        ///  - Before Redis 3.2.0, an error is returned if source and destination names are the same.<br /><br />
        /// <br />
        /// 将 Key 重命名为 New Key。如果键不存在，返回错误。<br />
        /// 如果 New Key 已存在，则将被覆盖，此时类似 DEL 命令，由于 RENAME 是 constant-time operation，因此当删除的键有很大的值时会有较大的延迟。<br />
        ///  - 在 Redis 3.2 之前，如果 Key 和 New Key 名字一样，将返回错误。<br /><br /> 
        /// <br />
        /// Document link: https://redis.io/commands/rename <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newkey">New key</param>
        public void Rename(string key, string newkey) => Call("RENAME".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// RENAMENX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Renames key to newkey. It returns an error when key does not exist. If newkey already exists it is overwritten, when this happens RENAME executes an implicit DEL operation, so if the deleted key contains a very big value it may cause high latency even if RENAME itself is usually a constant-time operation.<br />
        ///  - Before Redis 3.2.0, an error is returned if source and destination names are the same.<br /><br />
        /// <br />
        /// 将 Key 重命名为 New Key。如果键不存在，返回错误。<br />
        /// 如果 New Key 已存在，则将被覆盖，此时类似 DEL 命令，由于 RENAME 是 constant-time operation，因此当删除的键有很大的值时会有较大的延迟。<br />
        ///  - 在 Redis 3.2 之前，如果 Key 和 New Key 名字一样，将返回错误。<br /><br /> 
        /// <br />
        /// Document link: https://redis.io/commands/renamenx <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newkey">New key</param>
        /// <returns>specifically: True if key was renamed to newkey. False if newkey already exists.</returns>
        public bool RenameNx(string key, string newkey) => Call("RENAMENX".InputKey(key).InputKey(newkey), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// RESTORE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Create a key associated with a value that is obtained by deserializing the provided serialized value (obtained via DUMP).<br /><br />
        /// <br />
        /// 将经由 DUMP 命令序列化的值反序列化后，作为给定键的值进行保存。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/restore <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="serializedValue">Serialized value</param>
        public void Restore(string key, byte[] serializedValue) => Restore(key, 0, serializedValue);

        /// <summary>
        /// RESTORE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Create a key associated with a value that is obtained by deserializing the provided serialized value (obtained via DUMP).<br /><br />
        /// <br />
        /// 将经由 DUMP 命令序列化的值反序列化后，作为给定键的值进行保存。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/restore <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="ttl">If ttl is 0 the key is created without any expire, otherwise the specified expire time (in milliseconds) is set.</param>
        /// <param name="serializedValue">Serialized value</param>
        /// <param name="replace">REPLACE modifier: If the Key already exists, replace it. Available since 3.0.0.</param>
        /// <param name="absTtl">Absolute Unix timestamp (in milliseconds) in which the key will expire. Available since 5.0.0.</param>
        /// <param name="idleTimeSeconds">IDLETIME modifier. Available since 5.0.0.</param>
        /// <param name="frequency">FREQ modifier. Available since 5.0.0.</param>
        public void Restore(string key, int ttl, byte[] serializedValue, bool replace = false, bool absTtl = false, int? idleTimeSeconds = null, decimal? frequency = null) => Call("RESTORE"
            .InputKey(key, ttl)
            .InputRaw(serializedValue)
            .InputIf(replace, "REPLACE")
            .InputIf(absTtl, "ABSTTL")
            .InputIf(idleTimeSeconds != null, "IDLETIME", idleTimeSeconds)
            .InputIf(frequency != null, "FREQ", frequency), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// SCAN command (A Synchronized Version) <br /><br />
        /// <br />
        /// The SCAN command and the closely related commands SSCAN, HSCAN and ZSCAN are used in order to incrementally iterate over a collection of elements.<br /><br />
        /// <br />
        /// 使用 SCAN 命令及其关联的 SSCAN、HSCAN、ZSCAN 等命令来迭代返回元素集合。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/scan <br />
        /// Available since 2.8.0. 
        /// </summary>
        /// <param name="cursor">Cursor</param>
        /// <param name="pattern">MATCH option</param>
        /// <param name="count">COUNT option: while SCAN does not provide guarantees about the number of elements returned at every iteration, it is possible to empirically adjust the behavior of SCAN using the COUNT option, default is 10</param>
        /// <param name="type">TYPE option: the type argument is the same string name that the TYPE command returns. Available since 6.0</param>
        /// <returns>Return a two elements multi-bulk reply, where the first element is a string representing an unsigned 64 bit number (the cursor), and the second element is a multi-bulk with an array of elements.</returns>
        public ScanResult<string> Scan(long cursor, string pattern, long count, string type) => Call("SCAN"
            .Input(cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(!string.IsNullOrWhiteSpace(type), "TYPE", type), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public IEnumerable<string[]> Scan(string pattern, long count, string type) => new ScanCollection<string>(this, "scan", (cli, cursor) => cli.Scan(cursor, pattern, count, type));
        #region Scan IEnumerable
        class ScanCollection<T> : IEnumerable<T[]>
        {
            public IEnumerator<T[]> GetEnumerator()
            {
                long cursor = 0;
                if (_cli.Adapter.UseType == UseType.Cluster && _scanName == "scan")
                {
                    var cluster = (_cli.Adapter as ClusterAdapter);
                    var ibkeys = new List<string>();

                    #region get ibkeys
                    var testConnection = cluster._clusterConnectionStrings.FirstOrDefault();
                    var cnodes = _cli.Call("CLUSTER".SubCommand("NODES"), rt => rt.ThrowOrValue<string>()).Split('\n');
                    foreach (var cnode in cnodes)
                    {
                        if (string.IsNullOrEmpty(cnode)) continue;
                        var dt = cnode.Trim().Split(' ');
                        if (dt.Length < 9) continue;
                        if (!dt[2].StartsWith("master") && !dt[2].EndsWith("master")) continue;
                        if (dt[7] != "connected") continue;

                        var endpoint = dt[1];
                        var at40 = endpoint.IndexOf('@');
                        if (at40 != -1) endpoint = endpoint.Remove(at40);

                        if (endpoint.StartsWith("127.0.0.1"))
                            endpoint = $"{DefaultRedisSocket.SplitHost(testConnection.Host).Key}:{endpoint.Substring(10)}";
                        else if (endpoint.StartsWith("localhost", StringComparison.CurrentCultureIgnoreCase))
                            endpoint = $"{DefaultRedisSocket.SplitHost(testConnection.Host).Key}:{endpoint.Substring(10)}";
                        ibkeys.Add(endpoint);
                    }
                    #endregion

                    foreach (var poolkey in ibkeys)
                    {
                        cursor = 0;
                        var pool = cluster._ib.Get(poolkey);
                        if (pool?.IsAvailable != true) continue;
                        var cli = pool.Get();
                        var rds = cli.Value.Adapter.GetRedisSocket(null);
                        using (var rdsproxy = DefaultRedisSocket.CreateTempProxy(rds, () => pool.Return(cli)))
                        {
                            rdsproxy._poolkey = poolkey;
                            rdsproxy._pool = pool;

                            while (true)
                            {
                                var rt = _scanFunc(cli.Value, cursor);
                                cursor = rt.cursor;
                                if (rt.length > 0) yield return rt.items;
                                if (cursor <= 0) break;
                            }
                        }
                    }
                    yield break;
                }
                else
                {
                    while (true)
                    {
                        var rt = _scanFunc(_cli, cursor);
                        cursor = rt.cursor;
                        if (rt.length > 0) yield return rt.items;
                        if (cursor <= 0) break;
                    }
                    yield break;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            readonly RedisClient _cli;
            readonly string _scanName;
            readonly Func<RedisClient, long, ScanResult<T>> _scanFunc;
            public ScanCollection(RedisClient cli, string scanName, Func<RedisClient, long, ScanResult<T>> scanFunc)
            {
                _cli = cli;
                _scanName = scanName;
                _scanFunc = scanFunc;
            }
        }
        #endregion

        /// <summary>
        /// SORT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns or stores the elements contained in the list, set or sorted set at key. By default, sorting is numeric and elements are compared by their value interpreted as double precision floating point number. <br /><br />
        /// <br />
        /// 返回含在 LIST、SET、ZSET 中的元素。默认情况下，排序是数字形式的，并且将元素的值进行比较以解释为双精度浮点数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/sort <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Keys</param>
        /// <param name="byPattern">BY modifier</param>
        /// <param name="offset">The number of elements to skip.</param>
        /// <param name="count">Specifying the number of elements to return from starting at offset.</param>
        /// <param name="getPatterns">GET modifier</param>
        /// <param name="collation">ASC | DESC modifier</param>
        /// <param name="alpha">ALPHA modifier, sort by lexicographically.</param>
        /// <returns>A list of sorted elements</returns>
        public string[] Sort(string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] {"GET", a}).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA"), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// SORT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Stores the elements contained in the list, set or sorted set at key. By default, sorting is numeric and elements are compared by their value interpreted as double precision floating point number. <br /><br />
        /// <br />
        /// 存储包含在 LIST、SET、ZSET 中的元素。默认情况下，排序是数字形式的，并且将元素的值进行比较以解释为双精度浮点数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/sort <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="storeDestination">Storing the result of a SORT operation to the destination key.</param>
        /// <param name="key">Keys</param>
        /// <param name="byPattern">BY modifier</param>
        /// <param name="offset">The number of elements to skip.</param>
        /// <param name="count">Specifying the number of elements to return from starting at offset.</param>
        /// <param name="getPatterns">GET modifier</param>
        /// <param name="collation">ASC | DESC modifier</param>
        /// <param name="alpha">ALPHA modifier, sort by lexicographically.</param>
        /// <returns>The number of sorted elements in the destination list.</returns>
        public long SortStore(string storeDestination, string key, string byPattern = null, long offset = 0, long count = 0, string[] getPatterns = null, Collation? collation = null, bool alpha = false) => Call("SORT"
            .InputKey(key)
            .InputIf(!string.IsNullOrWhiteSpace(byPattern), "BY", byPattern)
            .InputIf(offset != 0 || count != 0, "LIMIT", offset, count)
            .InputIf(getPatterns?.Any() == true, getPatterns.Select(a => new[] {"GET", a}).SelectMany(a => a).ToArray())
            .InputIf(collation != null, collation)
            .InputIf(alpha, "ALPHA")
            .InputIf(!string.IsNullOrWhiteSpace(storeDestination), "STORE")
            .InputKeyIf(!string.IsNullOrWhiteSpace(storeDestination), storeDestination), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// TOUCH command (A Synchronized Version) <br /><br />
        /// <br />
        /// Alters the last access time of a key(s). A key is ignored if it does not exist.<br /><br />
        /// <br />
        /// 更改 Key(s) 最后访问时间。如果键不存在，则忽略之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/touch <br />
        /// Available since 3.2.1. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were touched.</returns>
        public long Touch(params string[] keys) => Call("TOUCH".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// TTL command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the remaining time to live of a key that has a timeout. This introspection capability allows a Redis client to check how many seconds a given key will continue to be part of the dataset.<br />
        /// In Redis 2.6 or older the command returns -1 if the key does not exist or if the key exist but has no associated expire. <br />
        /// Starting with Redis 2.8 the return value in case of error changed: <br />
        ///  - The command returns -2 if the key does not exist.<br />
        ///  - The command returns -1 if the key exists but has no associated expire.<br /><br />
        /// <br />
        /// 返回键的剩余生存时间。这种自省能力允许 Redis 客户端检查指定键还能再数据集中生存多少秒。<br />
        /// 在 Redis 2.6 之前，如果 Key 不存在或 Key 未设置过期时间，则返回 -1<br />
        /// 从 Redis 2.8 开始，将针对不同的错误情况返回不同的值：<br />
        ///  - 如果 Key 不存在，则返回 -2 <br />
        ///  - 如果 Key 存在，但没有设置过过期时间，则返回 -1 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/ttl <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>TTL in seconds, or a negative value in order to signal an error (see the description above).</returns>
        public long Ttl(string key) => Call("TTL".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// TYPE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the string representation of the type of the value stored at key. The different types that can be returned are: string, list, set, zset, hash and stream.<br /><br />
        /// <br />
        /// 获取键对应值的类型的字符串表达形式。可以返回的类型是：string，list，set，zset，hash 以及 stream。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/type <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The type of key, or none when key does not exist.</returns>
        public KeyType Type(string key) => Call("TYPE".InputKey(key), rt => rt.ThrowOrValue<KeyType>());

        /// <summary>
        /// UNLINK command (A Synchronized Version) <br /><br />
        /// <br />
        /// This command is very similar to DEL: it removes the specified keys. Just like DEL a key is ignored if it does not exist. However the command performs the actual memory reclaiming in a different thread, so it is not blocking, while DEL is. <br /><br />
        /// <br />
        /// 本命令与 DEL 相似，能删除指定的键值；如果键不存在，则忽略。与 DEL 不同的是，本命令将在另一个线程中执行实际的内存回收，因此是非阻塞的。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/unlink <br />
        /// Available since 4.0.0. 
        /// </summary>
        /// <param name="keys">Keys</param>
        /// <returns>The number of keys that were unlinked.</returns>
        public long UnLink(params string[] keys) => Call("UNLINK".InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// WAIT command (A Synchronized Version) <br /><br />
        /// <br />
        /// This command blocks the current client until all the previous write commands are successfully transferred and acknowledged by at least the specified number of replicas. If the timeout, specified in milliseconds, is reached, the command returns even if the specified number of replicas were not yet reached. <br /><br />
        /// <br />
        /// 本命令将阻塞当前客户端，直到所有写命令成功发送、且大于等于指定数量的副本进行了确认。<br />
        /// 如果超时（单位为毫秒），即便没能获得指定数量副本的确认，命令也会返回。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/wait <br />
        /// Available since 3.0.0. 
        /// </summary>
        /// <param name="numreplicas">The number of replicas</param>
        /// <param name="timeoutMilliseconds">Timeout milliseconds</param>
        /// <returns>The command returns the number of replicas reached by all the writes performed in the context of the current connection.</returns>
        public long Wait(long numreplicas, long timeoutMilliseconds) => Call("WAIT".Input(numreplicas, timeoutMilliseconds), rt => rt.ThrowOrValue<long>());
    }
}
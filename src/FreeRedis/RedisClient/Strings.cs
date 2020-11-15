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

        /// <summary>
        /// APPEND command (An Asynchronous Version)<br /><br />
        /// <br />
        /// If key already exists and is a string, this command appends the value at the end of the string. If key does not exist it is created and set as an empty string, so APPEND will be similar to SET in this special case. <br /><br />
        /// <br />
        /// 若键值已存在且为字符串，则此命令将值附加在字符串的末尾；<br />
        /// 若键值不存在，则会先创建一个空字符串，并附加值（在此情况下，APPEND 类似 SET 命令）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/append <br />
        /// Available since 2.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The length of the string after the append operation.</returns>
        public Task<long> AppendAsync<T>(string key, T value) => CallAsync("APPEND".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITCOUNT command (An Asynchronous Version)<br /><br />
        /// <br />
        /// Count the number of set bits (population counting) in a string.<br /><br />
        /// <br />
        /// 统计字符串中的位数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitcount <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Index of the start. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <param name="end">Index of the end. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <returns>The number of set bits in the string. Non-existent keys are treated as empty strings and will return zero.</returns>
        public Task<long> BitCountAsync(string key, long start, long end) => CallAsync("BITCOUNT".InputKey(key, start, end), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITOP command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Perform a bitwise operation between multiple keys (containing string values) and store the result in the destination key.<br /><br />
        /// <br />
        /// 在（包括字符串值）的多键之间按位运算，并将结果保存在目标键中。<br />
        /// 目前支持 AND、OR、XOR 与 NOT 四种运算方式。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitop <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="operation">Bit operation type: AND, OR, XOR or NOT</param>
        /// <param name="destkey">Destination Key</param>
        /// <param name="keys">Multiple keys (containing string values)</param>
        /// <returns>The size of the string stored in the destination key, that is equal to the size of the longest input string.</returns>
        public Task<long> BitOpAsync(BitOpOperation operation, string destkey, params string[] keys) => CallAsync("BITOP".InputRaw(operation).InputKey(destkey).InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITPOS command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Return the position of the first bit set to 1 or 0 in a string.<br />
        /// The position is returned, thinking of the string as an array of bits from left to right, where the first byte's most significant bit is at position 0, the second byte's most significant bit is at position 8, and so forth.<br /><br />
        /// <br />
        /// 返回字符串中第一个 1 或 0 的位置。<br />
        /// 注意，本命令将字符串视作位数组，自左向右计算，第一个字节在位置 0，第二个字节在位置 8，以此类推。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitpos <br />
        /// Available since 2.8.7.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="bit">Bit value, 1 is true, 0 is false.</param>
        /// <param name="start">Index of the start. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <param name="end">Index of the end. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <returns>Returns the position of the first bit set to 1 or 0 according to the request.</returns>
        public Task<long> BitPosAsync(string key, bool bit, long? start = null, long? end = null) => CallAsync("BITPOS"
                                                                                                               .InputKey(key, bit ? "1" : "0")
                                                                                                               .InputIf(start != null, start)
                                                                                                               .InputIf(end != null, start), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DECR command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by one. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值减去 1。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/decr <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>the value of key after the decrement.</returns>
        public Task<long> DecrAsync(string key) => CallAsync("DECR".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DECRBY command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by decrement. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值减去给定的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/decrby <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="decrement">The given value to be decreased.</param>
        /// <returns>the value of key after the decrement.</returns>
        public Task<long> DecrByAsync(string key, long decrement) => CallAsync("DECRBY".InputKey(key, decrement), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// GET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Get the value of key. If the key does not exist the special value nil is returned. An error is returned if the value stored at key is not a string, because GET only handles string values.<br /><br />
        /// <br />
        /// 获得给定键的值。若键不存在，则返回特殊的 nil 值。如果给定键的值不是字符串，则返回错误，因为 GET 指令只能处理字符串。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/get <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The value of key, or nil when key does not exist.</returns>
        public Task<string> GetAsync(string key) => CallAsync("GET".InputKey(key), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// GET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Get the value of key. If the key does not exist the special value nil is returned. An error is returned if the value stored at key is not a string, because GET only handles string values.<br /><br />
        /// <br />
        /// 获得给定键的值。若键不存在，则返回特殊的 nil 值。如果给定键的值不是字符串，则返回错误，因为 GET 指令只能处理字符串。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/get <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The value of key, or nil when key does not exist.</returns>
        public Task<T> GetAsync<T>(string key) => CallAsync("GET".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        /// <summary>
        /// GETBIT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the bit value at offset in the string value stored at key.<br /><br />
        /// <br />
        /// 返回键所对应字符串值中偏移量的位值。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getbit <br />
        /// Available since 2.2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset</param>
        /// <returns>The bit value stored at offset.</returns>
        public Task<bool> GetBitAsync(string key, long offset) => CallAsync("GETBIT".InputKey(key, offset), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// GETRANGE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the substring of the string value stored at key, determined by the offsets start and end (both are inclusive).<br /><br />
        /// <br />
        /// 返回键值的子字符串该字符串由偏移量 start 和 end 来确定（两端均闭包）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getrange <br />
        /// Available since 2.0.0. It is called SUBSTR in Redis versions &lt;= 2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Start</param>
        /// <param name="end">End</param>
        /// <returns>The substring of the string value</returns>
        public Task<string> GetRangeAsync(string key, long start, long end) => CallAsync("GETRANGE".InputKey(key, start, end), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// GETRANGE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the substring of the string value stored at key, determined by the offsets start and end (both are inclusive).<br /><br />
        /// <br />
        /// 返回键值的子字符串该字符串由偏移量 start 和 end 来确定（两端均闭包）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getrange <br />
        /// Available since 2.0.0. It is called SUBSTR in Redis versions &lt;= 2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Start</param>
        /// <param name="end">End</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<T> GetRangeAsync<T>(string key, long start, long end) => CallAsync("GETRANGE".InputKey(key, start, end).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        /// <summary>
        /// GETSET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Atomically sets key to value and returns the old value stored at key. Returns an error when key exists but does not hold a string value.<br /><br />
        /// <br />
        /// 以原子的方式将新值取代给定键的旧值，并返回旧值。如果该键存在但不包含字符串值时，返回错误。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getset <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">New value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The old value stored at key, or nil when key did not exist.</returns>
        public Task<string> GetSetAsync<T>(string key, T value) => CallAsync("GETSET".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// INCR command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Increments the number stored at key by one. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值加上 1。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incr <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The value of key after the increment</returns>
        public Task<long> IncrAsync(string key) => CallAsync("INCR".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// INCRBY command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by increment. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值加上给定的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incrby <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="increment">The given value to be increased.</param>
        /// <returns>The value of key after the increment</returns>
        public Task<long> IncrByAsync(string key, long increment) => CallAsync("INCRBY".InputKey(key, increment), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// INCRBYFLOAT command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Increment the string representing a floating point number stored at key by the specified increment. <br />
        /// By using a negative increment value, the result is that the value stored at the key is decremented (by the obvious properties of addition). <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if one of the following conditions occur:<br />
        /// - The key contains a value of the wrong type (not a string).<br />
        /// - The current key content or the specified increment are not parsable as a double precision floating point number.<br />
        /// If the command is successful the new incremented value is stored as the new value of the key (replacing the old one), and returned to the caller as a string.<br /><br />
        /// <br />
        /// 对该键的值加上给定的值，可以通过给定负值来减小对应键的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果发生以下情况，则返回错误：<br />
        /// - 键所对应的值是错误的类型（不是字符串）；<br />
        /// - 该键的内容不能被解析为双精度浮点数。<br />
        /// 如果命令执行成功，则将新值替换旧值，并返回给调用方。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incrby <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="increment">The given value to be increased.</param>
        /// <returns>The value of key after the increment.</returns>
        public Task<decimal> IncrByFloatAsync(string key, decimal increment) => CallAsync("INCRBYFLOAT".InputKey(key, increment), rt => rt.ThrowOrValue<decimal>());

        /// <summary>
        /// MGET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the values of all specified keys. For every key that does not hold a string value or does not exist, the special value nil is returned. Because of this, the operation never fails.<br /><br />
        /// <br />
        /// 返回所有给定键的值，对于其中个别键不存在，或其值不为字符串的，反悔特殊的 nil 值，因此 MGET 指令永远不会执行失败。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mget <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="keys">Key list</param>
        /// <returns>A list of values at the specified keys.</returns>
        public Task<string[]> MGetAsync(params string[] keys) => CallAsync("MGET".InputKey(keys), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// MGET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the values of all specified keys. For every key that does not hold a string value or does not exist, the special value nil is returned. Because of this, the operation never fails.<br /><br />
        /// <br />
        /// 返回所有给定键的值，对于其中个别键不存在，或其值不为字符串的，反悔特殊的 nil 值，因此 MGET 指令永远不会执行失败。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mget <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="keys">Key list</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A list of values at the specified keys.</returns>
        public Task<T[]> MGetAsync<T>(params string[] keys) => CallAsync("MGET".InputKey(keys).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        /// <summary>
        /// MSET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. <br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        public Task MSetAsync(string key, object value, params object[] keyValues) => MSetAsync<bool>(false, key, value, keyValues);

        /// <summary>
        /// MSET command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. <br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1.  
        /// </summary>
        /// <param name="keyValues">Key-value sets</param>
        /// <typeparam name="T"></typeparam>
        public Task MSetAsync<T>(Dictionary<string, T> keyValues) => CallAsync("MSET".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// MSETNX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSETNX will not perform any operation at all even if just a single key already exists.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。只要有一个键已经存在，则整组键值都将不会被设置。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/msetnx <br />
        /// Available since 1.0.1.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        /// <returns>True if the all the keys were set. False if no key was set (at least one key already existed).</returns>
        public Task<bool> MSetNxAsync(string key, object value, params object[] keyValues) => MSetAsync<bool>(true, key, value, keyValues);

        /// <summary>
        /// MSETNX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSETNX will not perform any operation at all even if just a single key already exists.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。只要有一个键已经存在，则整组键值都将不会被设置。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/msetnx <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="keyValues">Key-value sets</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>True if the all the keys were set. False if no key was set (at least one key already existed).</returns>
        public Task<bool> MSetNxAsync<T>(Dictionary<string, T> keyValues) => CallAsync("MSETNX".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// MSET key value [key value ...] command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. See MSETNX if you don't want to overwrite existing values.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。如果不想覆盖现有的值，可以使用 MSETNX 指令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="nx">Mark whether it is NX mode. If it is, use the MSETNX command; otherwise, use the MSET command.</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Always OK since MSET can't fail.</returns>
        Task<T> MSetAsync<T>(bool nx, string key, object value, params object[] keyValues)
        {
            if (keyValues?.Any() == true)
                return CallAsync((nx ? "MSETNX" : "MSET")
                                 .InputKey(key).InputRaw(SerializeRedisValue(value))
                                 .InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<T>());
            return CallAsync((nx ? "MSETNX" : "MSET").InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<T>());
        }

        /// <summary>
        /// PSETEX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// PSETEX works exactly like SETEX with the sole difference that the expire time is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// PSETEX 的工作方式与 SETEX 完全相同，唯一的区别是到期时间的单位是毫秒（ms），而不是秒（s）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/psetex <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="milliseconds">Timeout milliseconds value</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        public Task PSetExAsync<T>(string key, long milliseconds, T value) => CallAsync("PSETEX".InputKey(key, milliseconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        /// <summary>
        /// SET key value EX seconds (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value.<br /><br />
        /// <br />
        /// 设置键和值。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        public Task SetAsync<T>(string key, T value, int timeoutSeconds = 0) => SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false, false);

        /// <summary>
        /// SET key value KEEPTTL command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Retain the time to live associated with the key. <br /><br />
        /// <br />
        /// 设置键和值。<br /><br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 6.0.0.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <typeparam name="T"></typeparam>
        public Task SetAsync<T>(string key, T value, bool keepTtl) => SetAsync(key, value, TimeSpan.Zero, keepTtl, false, false, false);

        /// <summary>
        /// SET key value EX seconds NX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it does not already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值不存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        async public Task<bool> SetNxAsync<T>(string key, T value, int timeoutSeconds) => (await SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false, false)) == "OK";

        /// <summary>
        /// SET key value EX seconds XX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值已存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        async public Task<bool> SetXxAsync<T>(string key, T value, int timeoutSeconds = 0) => (await SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true, false)) == "OK";

        /// <summary>
        /// SET key value KEEPTTL XX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值已存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 6.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        async public Task<bool> SetXxAsync<T>(string key, T value, bool keepTtl) => (await SetAsync(key, value, TimeSpan.Zero, keepTtl, false, true, false)) == "OK";

        /// <summary>
        /// SET command (An Asynchronous Version) <br /><br />
        ///<br />
        /// Set key to hold the string value. If key already holds a value, it is overwritten, regardless of its type. <br /><br />
        /// <br />
        /// 设置键和值。如果该键已存在，则覆盖之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeout">Timeout value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <param name="nx">Only set the key if it does not already exist.</param>
        /// <param name="xx">Only set the key if it already exist.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        public Task<string> SetAsync<T>(string key, T value, TimeSpan timeout, bool keepTtl, bool nx, bool xx, bool get) => CallAsync("SET"
                                                                                                                                      .InputKey(key)
                                                                                                                                      .InputRaw(SerializeRedisValue(value))
                                                                                                                                      .InputIf(timeout.TotalSeconds >= 1, "EX", (long) timeout.TotalSeconds)
                                                                                                                                      .InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long) timeout.TotalMilliseconds)
                                                                                                                                      .InputIf(keepTtl, "KEEPTTL")
                                                                                                                                      .InputIf(nx, "NX")
                                                                                                                                      .InputIf(xx, "XX").InputIf(get, "GET"), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// SETBIT command (An Asynchronous Version) <br /><br />
        ///<br />
        /// Sets or clears the bit at offset in the string value stored at key.<br /><br />
        /// <br />
        /// 设置或清除键值字符串指定偏移量的位（bit）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setbit <br />
        /// Available since 2.2.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset value</param>
        /// <param name="value">New value</param>
        /// <returns>The original bit value stored at offset.</returns>
        public Task<long> SetBitAsync(string key, long offset, bool value) => CallAsync("SETBIT".InputKey(key, offset, value ? "1" : "0"), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// SETEX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold the string value and set key to timeout after a given number of seconds.<br /><br />
        /// <br />
        /// 设置键值在给定的秒数后超时。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setex <br />
        /// Available since 2.0.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="seconds">Seconds</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        public Task SetExAsync<T>(string key, int seconds, T value) => CallAsync("SETEX".InputKey(key, seconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        /// <summary>
        /// SETNX command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Set key to hold string value if key does not exist. In that case, it is equal to SET. When key already holds a value, no operation is performed. <br />
        /// SETNX is short for "SET if Not eXists".<br /><br />
        /// <br />
        /// 如果键值不存在，则设置该键值，在此情况下与 SET 指令相似。当键值已经存在时，不执行任何操作。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setnx <br />
        /// Available since 1.0.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Set result, specifically: 1 is if the key was set; 0 is if the key was not set.</returns>
        public Task<bool> SetNxAsync<T>(string key, T value) => CallAsync("SETNX".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// SETRANGE command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Overwrites part of the string stored at key, starting at the specified offset, for the entire length of value. If the offset is larger than the current length of the string at key, the string is padded with zero-bytes to make offset fit. Non-existing keys are considered as empty strings, so this command will make sure it holds a string large enough to be able to set value at offset.<br /><br />
        /// <br />
        /// 从给定的偏移量开始覆盖键所对应的字符串值。如果偏移量大于值的长度，则为该字符串填充零字节（zero-bytes）以满足偏移量的要求。若键值不存在则视作空字符串值。故本指令将确保有足够长度的字符串以适应偏移量的要求。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setrange <br />
        /// Available since 2.2.0.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset value</param>
        /// <param name="value">The value to be filled.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The length of the string after it was modified by the command.</returns>
        public Task<long> SetRangeAsync<T>(string key, long offset, T value) => CallAsync("SETRANGE".InputKey(key, offset).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());

        //STRALGO LCS algo-specific-argument [algo-specific-argument ...]

        /// <summary>
        /// STRLRN command (An Asynchronous Version) <br /><br />
        /// <br />
        /// Returns the length of the string value stored at key. An error is returned when key holds a non-string value.<br /><br />
        /// <br />
        /// 返回键对应值字符串的长度。当该键对应的值不是字符串，则返回错误。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/strlen <br />
        /// Available since 2.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The length of the string at key, or 0 when key does not exist.</returns>
        public Task<long> StrLenAsync(string key) => CallAsync("STRLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        #endregion

#endif

        /// <summary>
        /// APPEND command (A Synchronized Version)<br /><br />
        /// <br />
        /// If key already exists and is a string, this command appends the value at the end of the string. If key does not exist it is created and set as an empty string, so APPEND will be similar to SET in this special case. <br /><br />
        /// <br />
        /// 若键值已存在且为字符串，则此命令将值附加在字符串的末尾；<br />
        /// 若键值不存在，则会先创建一个空字符串，并附加值（在此情况下，APPEND 类似 SET 命令）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/append <br />
        /// Available since 2.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The length of the string after the append operation.</returns>
        public long Append<T>(string key, T value) => Call("APPEND".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITCOUNT command (A Synchronized Version)<br /><br />
        /// <br />
        /// Count the number of set bits (population counting) in a string.<br /><br />
        /// <br />
        /// 统计字符串中的位数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitcount <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Index of the start. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <param name="end">Index of the end. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <returns>The number of set bits in the string. Non-existent keys are treated as empty strings and will return zero.</returns>
        public long BitCount(string key, long start, long end) => Call("BITCOUNT".InputKey(key, start, end), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITOP command (A Synchronized Version) <br /><br />
        /// <br />
        /// Perform a bitwise operation between multiple keys (containing string values) and store the result in the destination key.<br /><br />
        /// <br />
        /// 在（包括字符串值）的多键之间按位运算，并将结果保存在目标键中。<br />
        /// 目前支持 AND、OR、XOR 与 NOT 四种运算方式。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitop <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="operation">Bit operation type: AND, OR, XOR or NOT</param>
        /// <param name="destkey">Destination Key</param>
        /// <param name="keys">Multiple keys (containing string values)</param>
        /// <returns>The size of the string stored in the destination key, that is equal to the size of the longest input string.</returns>
        public long BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call("BITOP".InputRaw(operation).InputKey(destkey).InputKey(keys), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// BITPOS command (A Synchronized Version) <br /><br />
        /// <br />
        /// Return the position of the first bit set to 1 or 0 in a string.<br />
        /// The position is returned, thinking of the string as an array of bits from left to right, where the first byte's most significant bit is at position 0, the second byte's most significant bit is at position 8, and so forth.<br /><br />
        /// <br />
        /// 返回字符串中第一个 1 或 0 的位置。<br />
        /// 注意，本命令将字符串视作位数组，自左向右计算，第一个字节在位置 0，第二个字节在位置 8，以此类推。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/bitpos <br />
        /// Available since 2.8.7.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="bit">Bit value, 1 is true, 0 is false.</param>
        /// <param name="start">Index of the start. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <param name="end">Index of the end. It can contain negative values in order to index bytes starting from the end of the string.</param>
        /// <returns>Returns the position of the first bit set to 1 or 0 according to the request.</returns>
        public long BitPos(string key, bool bit, long? start = null, long? end = null) => Call("BITPOS"
                                                                                               .InputKey(key, bit ? "1" : "0")
                                                                                               .InputIf(start != null, start)
                                                                                               .InputIf(end != null, start), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DECR command (A Synchronized Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by one. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值减去 1。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/decr <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>the value of key after the decrement.</returns>
        public long Decr(string key) => Call("DECR".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// DECRBY command (A Synchronized Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by decrement. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值减去给定的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/decrby <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="decrement">The given value to be decreased.</param>
        /// <returns>the value of key after the decrement.</returns>
        public long DecrBy(string key, long decrement) => Call("DECRBY".InputKey(key, decrement), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// GET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Get the value of key. If the key does not exist the special value nil is returned. An error is returned if the value stored at key is not a string, because GET only handles string values.<br /><br />
        /// <br />
        /// 获得给定键的值。若键不存在，则返回特殊的 nil 值。如果给定键的值不是字符串，则返回错误，因为 GET 指令只能处理字符串。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/get <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The value of key, or nil when key does not exist.</returns>
        public string Get(string key) => Call("GET".InputKey(key), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// GET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Get the value of key. If the key does not exist the special value nil is returned. An error is returned if the value stored at key is not a string, because GET only handles string values.<br /><br />
        /// <br />
        /// 获得给定键的值。若键不存在，则返回特殊的 nil 值。如果给定键的值不是字符串，则返回错误，因为 GET 指令只能处理字符串。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/get <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The value of key, or nil when key does not exist.</returns>
        public T Get<T>(string key) => Call("GET".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        /// <summary>
        /// GET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Get the value of key and write to the stream.. If the key does not exist the special value nil is returned. An error is returned if the value stored at key is not a string, because GET only handles string values.<br /><br />
        /// <br />
        /// 获得给定键的值并写入流中。若键不存在，则返回特殊的 nil 值。如果给定键的值不是字符串，则返回错误，因为 GET 指令只能处理字符串。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/get <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="destination">Destination stream</param>
        /// <param name="bufferSize">Size</param>
        public void Get(string key, Stream destination, int bufferSize = 1024)
        {
            var cmd = "GET".InputKey(key);
            Adapter.TopOwner.LogCall(cmd, () =>
            {
                using (var rds = Adapter.GetRedisSocket(cmd))
                {
                    rds.Write(cmd);
                    rds.ReadChunk(destination, bufferSize);
                }

                return default(string);
            });
        }

        /// <summary>
        /// GETBIT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the bit value at offset in the string value stored at key.<br /><br />
        /// <br />
        /// 返回键所对应字符串值中偏移量的位值。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getbit <br />
        /// Available since 2.2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset</param>
        /// <returns>The bit value stored at offset.</returns>
        public bool GetBit(string key, long offset) => Call("GETBIT".InputKey(key, offset), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// GETRANGE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the substring of the string value stored at key, determined by the offsets start and end (both are inclusive).<br /><br />
        /// <br />
        /// 返回键值的子字符串该字符串由偏移量 start 和 end 来确定（两端均闭包）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getrange <br />
        /// Available since 2.0.0. It is called SUBSTR in Redis versions &lt;= 2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Start</param>
        /// <param name="end">End</param>
        /// <returns>The substring of the string value</returns>
        public string GetRange(string key, long start, long end) => Call("GETRANGE".InputKey(key, start, end), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// GETRANGE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the substring of the string value stored at key, determined by the offsets start and end (both are inclusive).<br /><br />
        /// <br />
        /// 返回键值的子字符串该字符串由偏移量 start 和 end 来确定（两端均闭包）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getrange <br />
        /// Available since 2.0.0. It is called SUBSTR in Redis versions &lt;= 2.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="start">Start</param>
        /// <param name="end">End</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRange<T>(string key, long start, long end) => Call("GETRANGE".InputKey(key, start, end).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        /// <summary>
        /// GETSET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Atomically sets key to value and returns the old value stored at key. Returns an error when key exists but does not hold a string value.<br /><br />
        /// <br />
        /// 以原子的方式将新值取代给定键的旧值，并返回旧值。如果该键存在但不包含字符串值时，返回错误。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/getset <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">New value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The old value stored at key, or nil when key did not exist.</returns>
        public string GetSet<T>(string key, T value) => Call("GETSET".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// INCR command (A Synchronized Version) <br /><br />
        /// <br />
        /// Increments the number stored at key by one. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值加上 1。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incr <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The value of key after the increment</returns>
        public long Incr(string key) => Call("INCR".InputKey(key), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// INCRBY command (A Synchronized Version) <br /><br />
        /// <br />
        /// Decrements the number stored at key by increment. <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if the key contains a value of the wrong type or contains a string that can not be represented as integer. <br />
        /// This operation is limited to 64 bit signed integers.<br /><br />
        /// <br />
        /// 对该键的值加上给定的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果键对应的值是个错误类型或不能表达为整数的字符串，则返回错误。此操作仅限于 64 位有符号整数。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incrby <br />
        /// Available since 1.0.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="increment">The given value to be increased.</param>
        /// <returns>The value of key after the increment</returns>
        public long IncrBy(string key, long increment) => Call("INCRBY".InputKey(key, increment), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// INCRBYFLOAT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Increment the string representing a floating point number stored at key by the specified increment. <br />
        /// By using a negative increment value, the result is that the value stored at the key is decremented (by the obvious properties of addition). <br />
        /// If the key does not exist, it is set to 0 before performing the operation. An error is returned if one of the following conditions occur:<br />
        /// - The key contains a value of the wrong type (not a string).<br />
        /// - The current key content or the specified increment are not parsable as a double precision floating point number.<br />
        /// If the command is successful the new incremented value is stored as the new value of the key (replacing the old one), and returned to the caller as a string.<br /><br />
        /// <br />
        /// 对该键的值加上给定的值，可以通过给定负值来减小对应键的值。<br />
        /// 如果键值不存在，则在操作前先设置为 0。如果发生以下情况，则返回错误：<br />
        /// - 键所对应的值是错误的类型（不是字符串）；<br />
        /// - 该键的内容不能被解析为双精度浮点数。<br />
        /// 如果命令执行成功，则将新值替换旧值，并返回给调用方。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/incrby <br />
        /// Available since 2.6.0.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="increment">The given value to be increased.</param>
        /// <returns>The value of key after the increment.</returns>
        public decimal IncrByFloat(string key, decimal increment) => Call("INCRBYFLOAT".InputKey(key, increment), rt => rt.ThrowOrValue<decimal>());

        /// <summary>
        /// MGET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the values of all specified keys. For every key that does not hold a string value or does not exist, the special value nil is returned. Because of this, the operation never fails.<br /><br />
        /// <br />
        /// 返回所有给定键的值，对于其中个别键不存在，或其值不为字符串的，反悔特殊的 nil 值，因此 MGET 指令永远不会执行失败。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mget <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="keys">Key list</param>
        /// <returns>A list of values at the specified keys.</returns>
        public string[] MGet(params string[] keys) => Call("MGET".InputKey(keys), rt => rt.ThrowOrValue<string[]>());

        /// <summary>
        /// MGET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the values of all specified keys. For every key that does not hold a string value or does not exist, the special value nil is returned. Because of this, the operation never fails.<br /><br />
        /// <br />
        /// 返回所有给定键的值，对于其中个别键不存在，或其值不为字符串的，反悔特殊的 nil 值，因此 MGET 指令永远不会执行失败。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mget <br />
        /// Available since 1.0.0. 
        /// </summary>
        /// <param name="keys">Key list</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A list of values at the specified keys.</returns>
        public T[] MGet<T>(params string[] keys) => Call("MGET".InputKey(keys).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        /// <summary>
        /// MSET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. <br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        public void MSet(string key, object value, params object[] keyValues) => MSet<bool>(false, key, value, keyValues);

        /// <summary>
        /// MSET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. <br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1.  
        /// </summary>
        /// <param name="keyValues">Key-value sets</param>
        /// <typeparam name="T"></typeparam>
        public void MSet<T>(Dictionary<string, T> keyValues) => Call("MSET".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// MSETNX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSETNX will not perform any operation at all even if just a single key already exists.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。只要有一个键已经存在，则整组键值都将不会被设置。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/msetnx <br />
        /// Available since 1.0.1.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        /// <returns>True if the all the keys were set. False if no key was set (at least one key already existed).</returns>
        public bool MSetNx(string key, object value, params object[] keyValues) => MSet<bool>(true, key, value, keyValues);

        /// <summary>
        /// MSETNX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSETNX will not perform any operation at all even if just a single key already exists.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。只要有一个键已经存在，则整组键值都将不会被设置。 <br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/msetnx <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="keyValues">Key-value sets</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>True if the all the keys were set. False if no key was set (at least one key already existed).</returns>
        public bool MSetNx<T>(Dictionary<string, T> keyValues) => Call("MSETNX".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// MSET key value [key value ...] command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets the given keys to their respective values. MSET replaces existing values with new values, just as regular SET. See MSETNX if you don't want to overwrite existing values.<br /><br />
        /// <br />
        /// 将给定键的值设置为对应的新值。如果不想覆盖现有的值，可以使用 MSETNX 指令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/mset <br />
        /// Available since 1.0.1. 
        /// </summary>
        /// <param name="nx">Mark whether it is NX mode. If it is, use the MSETNX command; otherwise, use the MSET command.</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keyValues">Other key-value sets</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Always OK since MSET can't fail.</returns>
        T MSet<T>(bool nx, string key, object value, params object[] keyValues)
        {
            if (keyValues?.Any() == true)
                return Call((nx ? "MSETNX" : "MSET")
                            .InputKey(key).InputRaw(SerializeRedisValue(value))
                            .InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<T>());
            return Call((nx ? "MSETNX" : "MSET").InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<T>());
        }

        /// <summary>
        /// PSETEX command (A Synchronized Version) <br /><br />
        /// <br />
        /// PSETEX works exactly like SETEX with the sole difference that the expire time is specified in milliseconds instead of seconds.<br /><br />
        /// <br />
        /// PSETEX 的工作方式与 SETEX 完全相同，唯一的区别是到期时间的单位是毫秒（ms），而不是秒（s）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/psetex <br />
        /// Available since 2.6.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="milliseconds">Timeout milliseconds value</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        public void PSetEx<T>(string key, long milliseconds, T value) => Call("PSETEX".InputKey(key, milliseconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        /// <summary>
        /// SET key value EX seconds (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value.<br /><br />
        /// <br />
        /// 设置键和值。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        public void Set<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false, false);

        /// <summary>
        /// SET key value KEEPTTL command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Retain the time to live associated with the key. <br /><br />
        /// <br />
        /// 设置键和值。<br /><br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 6.0.0.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <typeparam name="T"></typeparam>
        public void Set<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, false, false);

        /// <summary>
        /// SET key value EX seconds NX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it does not already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值不存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        public bool SetNx<T>(string key, T value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false, false) == "OK";

        /// <summary>
        /// SET key value EX seconds XX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值已存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 2.6.12. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeoutSeconds">Timeout seconds</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        public bool SetXx<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true, false) == "OK";

        /// <summary>
        /// SET key value KEEPTTL XX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. Only set the key if it already exist.<br /><br />
        /// <br />
        /// 设置键和值。当且仅当键值已存在时才执行命令。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 6.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        public bool SetXx<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, true, false) == "OK";

        /// <summary>
        /// SET command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value. If key already holds a value, it is overwritten, regardless of its type. <br /><br />
        /// <br />
        /// 设置键和值。如果该键已存在，则覆盖之。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/set <br />
        /// Available since 6.0.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeout">Timeout value</param>
        /// <param name="keepTtl">Retain the time to live associated with the key</param>
        /// <param name="nx">Only set the key if it does not already exist.</param>
        /// <param name="xx">Only set the key if it already exist.</param>
        /// <param name="get">Return the old value stored at key, or nil when key did not exist.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Simple string reply: OK if SET was executed correctly.<br />
        /// Bulk string reply: when GET option is set, the old value stored at key, or nil when key did not exist.<br />
        /// Null reply: a Null Bulk Reply is returned if the SET operation was not performed because the user specified the NX or XX option but the condition was not met or if user specified the NX and GET options that do not met.
        /// </returns>
        public string Set<T>(string key, T value, TimeSpan timeout, bool keepTtl, bool nx, bool xx, bool get) => Call("SET"
                                                                                                                      .InputKey(key)
                                                                                                                      .InputRaw(SerializeRedisValue(value))
                                                                                                                      .InputIf(timeout.TotalSeconds >= 1, "EX", (long) timeout.TotalSeconds)
                                                                                                                      .InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long) timeout.TotalMilliseconds)
                                                                                                                      .InputIf(keepTtl, "KEEPTTL")
                                                                                                                      .InputIf(nx, "NX")
                                                                                                                      .InputIf(xx, "XX")
                                                                                                                      .InputIf(get, "GET"), rt => rt.ThrowOrValue<string>());

        /// <summary>
        /// SETBIT command (A Synchronized Version) <br /><br />
        /// <br />
        /// Sets or clears the bit at offset in the string value stored at key.<br /><br />
        /// <br />
        /// 设置或清除键值字符串指定偏移量的位（bit）。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setbit <br />
        /// Available since 2.2.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset value</param>
        /// <param name="value">New value</param>
        /// <returns>The original bit value stored at offset.</returns>
        public long SetBit(string key, long offset, bool value) => Call("SETBIT".InputKey(key, offset, value ? "1" : "0"), rt => rt.ThrowOrValue<long>());

        /// <summary>
        /// SETEX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold the string value and set key to timeout after a given number of seconds.<br /><br />
        /// <br />
        /// 设置键值在给定的秒数后超时。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setex <br />
        /// Available since 2.0.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="seconds">Seconds</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        public void SetEx<T>(string key, int seconds, T value) => Call("SETEX".InputKey(key, seconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        /// <summary>
        /// SETNX command (A Synchronized Version) <br /><br />
        /// <br />
        /// Set key to hold string value if key does not exist. In that case, it is equal to SET. When key already holds a value, no operation is performed. <br />
        /// SETNX is short for "SET if Not eXists".<br /><br />
        /// <br />
        /// 如果键值不存在，则设置该键值，在此情况下与 SET 指令相似。当键值已经存在时，不执行任何操作。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setnx <br />
        /// Available since 1.0.0.   
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Set result, specifically: 1 is if the key was set; 0 is if the key was not set.</returns>
        public bool SetNx<T>(string key, T value) => Call("SETNX".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());

        /// <summary>
        /// SETRANGE command (A Synchronized Version) <br /><br />
        /// <br />
        /// Overwrites part of the string stored at key, starting at the specified offset, for the entire length of value. If the offset is larger than the current length of the string at key, the string is padded with zero-bytes to make offset fit. Non-existing keys are considered as empty strings, so this command will make sure it holds a string large enough to be able to set value at offset.<br /><br />
        /// <br />
        /// 从给定的偏移量开始覆盖键所对应的字符串值。如果偏移量大于值的长度，则为该字符串填充零字节（zero-bytes）以满足偏移量的要求。若键值不存在则视作空字符串值。故本指令将确保有足够长度的字符串以适应偏移量的要求。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/setrange <br />
        /// Available since 2.2.0.  
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="offset">Offset value</param>
        /// <param name="value">The value to be filled.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The length of the string after it was modified by the command.</returns>
        public long SetRange<T>(string key, long offset, T value) => Call("SETRANGE".InputKey(key, offset).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());

        //STRALGO LCS algo-specific-argument [algo-specific-argument ...]

        /// <summary>
        /// STRLRN command (A Synchronized Version) <br /><br />
        /// <br />
        /// Returns the length of the string value stored at key. An error is returned when key holds a non-string value.<br /><br />
        /// <br />
        /// 返回键对应值字符串的长度。当该键对应的值不是字符串，则返回错误。<br /><br />
        /// <br />
        /// Document link: https://redis.io/commands/strlen <br />
        /// Available since 2.2.0. 
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>The length of the string at key, or 0 when key does not exist.</returns>
        public long StrLen(string key) => Call("STRLEN".InputKey(key), rt => rt.ThrowOrValue<long>());
    }
}
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
        public Task<long> SAddAsync(string key, params object[] members) => CallAsync("SADD".InputKey(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public Task<long> SCardAsync(string key) => CallAsync("SCARD".InputKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> SDiffAsync(params string[] keys) => CallAsync("SDIFF".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SDiffAsync<T>(params string[] keys) => SReadArrayAsync<T>("SDIFF".InputKey(keys));
        public Task<long> SDiffStoreAsync(string destination, params string[] keys) => CallAsync("SDIFFSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        public Task<string[]> SInterAsync(params string[] keys) => CallAsync("SINTER".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SInterAsync<T>(params string[] keys) => SReadArrayAsync<T>("SINTER".InputKey(keys));
        public Task<long> SInterStoreAsync(string destination, params string[] keys) => CallAsync("SINTERSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        public Task<bool> SIsMemberAsync<T>(string key, T member) => CallAsync("SISMEMBER".InputKey(key).InputRaw(SerializeRedisValue(member)), rt => rt.ThrowOrValue<bool>());
        public Task<string[]> SMeMembersAsync(string key) => CallAsync("SMEMBERS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SMeMembersAsync<T>(string key) => SReadArrayAsync<T>("SMISMEMBER".InputKey(key));

        public Task<bool> SMoveAsync<T>(string source, string destination, T member) => CallAsync("SMOVE"
            .InputKey(source)
            .InputKey(destination)
            .InputRaw(SerializeRedisValue(member)), rt => rt.ThrowOrValue<bool>());

        public Task<string> SPopAsync(string key) => CallAsync("SPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> SPopAsync<T>(string key) => CallAsync("SPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public Task<string[]> SPopAsync(string key, int count) => CallAsync("SPOP".InputKey(key, count), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SPopAsync<T>(string key, int count) => SReadArrayAsync<T>("SPOP".InputKey(key, count));

        public Task<string> SRandMemberAsync(string key) => CallAsync("SRANDMEMBER".InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> SRandMemberAsync<T>(string key) => CallAsync("SRANDMEMBER".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public Task<string[]> SRandMemberAsync(string key, int count) => CallAsync("SRANDMEMBER".InputKey(key, count), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SRandMemberAsync<T>(string key, int count) => SReadArrayAsync<T>("SRANDMEMBER".InputKey(key, count));

        public Task<long> SRemAsync(string key, params object[] members) => CallAsync("SREM".InputKey(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public Task<ScanResult<string>> SScanAsync(string key, long cursor, string pattern, long count) => CallAsync("SSCAN"
            .InputKey(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<string[]> SUnionAsync(params string[] keys) => CallAsync("SUNION".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SUnionAsync<T>(params string[] keys) => SReadArrayAsync<T>("SUNION".InputKey(keys));
        public Task<long> SUnionStoreAsync(string destination, params string[] keys) => CallAsync("SUNIONSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        Task<T[]> SReadArrayAsync<T>(CommandPacket cb) => CallAsync(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new T[0] : a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
        #endregion
#endif

        public long SAdd(string key, params object[] members) => Call("SADD".InputKey(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public long SCard(string key) => Call("SCARD".InputKey(key), rt => rt.ThrowOrValue<long>());

        public string[] SDiff(params string[] keys) => Call("SDIFF".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SDiff<T>(params string[] keys) => SReadArray<T>("SDIFF".InputKey(keys));
        public long SDiffStore(string destination, params string[] keys) => Call("SDIFFSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        public string[] SInter(params string[] keys) => Call("SINTER".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SInter<T>(params string[] keys) => SReadArray<T>("SINTER".InputKey(keys));
        public long SInterStore(string destination, params string[] keys) => Call("SINTERSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        public bool SIsMember<T>(string key, T member) => Call("SISMEMBER".InputKey(key).InputRaw(SerializeRedisValue(member)), rt => rt.ThrowOrValue<bool>());
        public string[] SMeMembers(string key) => Call("SMEMBERS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] SMeMembers<T>(string key) => SReadArray<T>("SMISMEMBER".InputKey(key));

        public bool SMove<T>(string source, string destination, T member) => Call("SMOVE"
            .InputKey(source)
            .InputKey(destination)
            .InputRaw(SerializeRedisValue(member)), rt => rt.ThrowOrValue<bool>());

        public string SPop(string key) => Call("SPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public T SPop<T>(string key) => Call("SPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public string[] SPop(string key, int count) => Call("SPOP".InputKey(key, count), rt => rt.ThrowOrValue<string[]>());
        public T[] SPop<T>(string key, int count) => SReadArray<T>("SPOP".InputKey(key, count));

        public string SRandMember(string key) => Call("SRANDMEMBER".InputKey(key), rt => rt.ThrowOrValue<string>());
        public T SRandMember<T>(string key) => Call("SRANDMEMBER".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public string[] SRandMember(string key, int count) => Call("SRANDMEMBER".InputKey(key, count), rt => rt.ThrowOrValue<string[]>());
        public T[] SRandMember<T>(string key, int count) => SReadArray<T>("SRANDMEMBER".InputKey(key, count));

        public long SRem(string key, params object[] members) => Call("SREM".InputKey(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public ScanResult<string> SScan(string key, long cursor, string pattern, long count) => Call("SSCAN"
            .InputKey(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public string[] SUnion(params string[] keys) => Call("SUNION".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SUnion<T>(params string[] keys) => SReadArray<T>("SUNION".InputKey(keys));
        public long SUnionStore(string destination, params string[] keys) => Call("SUNIONSTORE".InputKey(destination).InputKey(keys), rt => rt.ThrowOrValue<long>());

        T[] SReadArray<T>(CommandPacket cb) => Call(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new T[0] : a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}

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
        public Task<long> SAddAsync(string key, params object[] members) => CallAsync("SADD".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> SCardAsync(string key) => CallAsync("SCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> SDiffAsync(params string[] keys) => CallAsync("SDIFF".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SDiffAsync<T>(params string[] keys) => SReadArrayAsync<T>("SDIFF".Input(keys).FlagKey(keys));
        public Task<long> SDiffStoreAsync(string destination, params string[] keys) => CallAsync("SDIFFSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public Task<string[]> SInterAsync(params string[] keys) => CallAsync("SINTER".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SInterAsync<T>(params string[] keys) => SReadArrayAsync<T>("SINTER".Input(keys).FlagKey(keys));
        public Task<long> SInterStoreAsync(string destination, params string[] keys) => CallAsync("SINTERSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public Task<bool> SIsMemberAsync<T>(string key, T member) => CallAsync("SISMEMBER".Input(key).InputRaw(SerializeRedisValue(member)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<string[]> SMeMembersAsync(string key) => CallAsync("SMEMBERS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SMeMembersAsync<T>(string key) => SReadArrayAsync<T>("SMISMEMBER".Input(key).FlagKey(key));

        public Task<bool> SMoveAsync<T>(string source, string destination, T member) => CallAsync("SMOVE"
            .Input(source, destination)
            .InputRaw(SerializeRedisValue(member))
            .FlagKey(new[] { source, destination }), rt => rt.ThrowOrValue<bool>());

        public Task<string> SPopAsync(string key) => CallAsync("SPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> SPopAsync<T>(string key) => CallAsync("SPOP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public Task<string[]> SPopAsync(string key, int count) => CallAsync("SPOP".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SPopAsync<T>(string key, int count) => SReadArrayAsync<T>("SPOP".Input(key, count).FlagKey(key));

        public Task<string> SRandMemberAsync(string key) => CallAsync("SRANDMEMBER".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> SRandMemberAsync<T>(string key) => CallAsync("SRANDMEMBER".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public Task<string[]> SRandMemberAsync(string key, int count) => CallAsync("SRANDMEMBER".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SRandMemberAsync<T>(string key, int count) => SReadArrayAsync<T>("SRANDMEMBER".Input(key, count).FlagKey(key));

        public Task<long> SRemAsync(string key, params object[] members) => CallAsync("SREM".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<ScanResult<string>> SScanAsync(string key, long cursor, string pattern, long count) => CallAsync("SSCAN"
            .Input(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<string[]> SUnionAsync(params string[] keys) => CallAsync("SUNION".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> SUnionAsync<T>(params string[] keys) => SReadArrayAsync<T>("SUNION".Input(keys).FlagKey(keys));
        public Task<long> SUnionStoreAsync(string destination, params string[] keys) => CallAsync("SUNIONSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        Task<T[]> SReadArrayAsync<T>(CommandPacket cb) => CallAsync(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new T[0] : a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
        #endregion
#endif

        public long SAdd(string key, params object[] members) => Call("SADD".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long SCard(string key) => Call("SCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[] SDiff(params string[] keys) => Call("SDIFF".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SDiff<T>(params string[] keys) => SReadArray<T>("SDIFF".Input(keys).FlagKey(keys));
        public long SDiffStore(string destination, params string[] keys) => Call("SDIFFSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public string[] SInter(params string[] keys) => Call("SINTER".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SInter<T>(params string[] keys) => SReadArray<T>("SINTER".Input(keys).FlagKey(keys));
        public long SInterStore(string destination, params string[] keys) => Call("SINTERSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        public bool SIsMember<T>(string key, T member) => Call("SISMEMBER".Input(key).InputRaw(SerializeRedisValue(member)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public string[] SMeMembers(string key) => Call("SMEMBERS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] SMeMembers<T>(string key) => SReadArray<T>("SMISMEMBER".Input(key).FlagKey(key));

        public bool SMove<T>(string source, string destination, T member) => Call("SMOVE"
            .Input(source, destination)
            .InputRaw(SerializeRedisValue(member))
            .FlagKey(new[] { source, destination }), rt => rt.ThrowOrValue<bool>());

        public string SPop(string key) => Call("SPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T SPop<T>(string key) => Call("SPOP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public string[] SPop(string key, int count) => Call("SPOP".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] SPop<T>(string key, int count) => SReadArray<T>("SPOP".Input(key, count).FlagKey(key));

        public string SRandMember(string key) => Call("SRANDMEMBER".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T SRandMember<T>(string key) => Call("SRANDMEMBER".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public string[] SRandMember(string key, int count) => Call("SRANDMEMBER".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] SRandMember<T>(string key, int count) => SReadArray<T>("SRANDMEMBER".Input(key, count).FlagKey(key));

        public long SRem(string key, params object[] members) => Call("SREM".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public ScanResult<string> SScan(string key, long cursor, string pattern, long count) => Call("SSCAN"
            .Input(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public string[] SUnion(params string[] keys) => Call("SUNION".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] SUnion<T>(params string[] keys) => SReadArray<T>("SUNION".Input(keys).FlagKey(keys));
        public long SUnionStore(string destination, params string[] keys) => Call("SUNIONSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue<long>());

        T[] SReadArray<T>(CommandPacket cb) => Call(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new T[0] : a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}

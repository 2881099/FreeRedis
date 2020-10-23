using FreeRedis.Internal;
using FreeRedis.Model;
using FreeRedis.Model.Sentinel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FreeRedis
{
    public partial class RedisSentinelClient : IDisposable
    {
        readonly IRedisSocket _redisSocket;

        public RedisSentinelClient(string host, bool ssl = false)
        {
            _redisSocket = new DefaultRedisSocket(host, ssl);
        }

        ~RedisSentinelClient() => this.Dispose();
        int _disposeCounter;
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCounter) != 1) return;
            try
            {
                _redisSocket.Dispose();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        protected T2 Call<T2>(CommandPacket cmd, Func<RedisResult<T2>, T2> parse) => Call<T2, T2>(cmd, parse);
        protected T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
        {
            _redisSocket.Write(cmd);
            var result = cmd.Read<T1>();
            return parse(result);
        }

        public string Ping() => Call<string>("PING", rt => rt.ThrowOrValue());
        public InfoResult Info() => Call<string, InfoResult>("INFO", rt => rt.NewValue(a => new InfoResult(a)).ThrowOrValue());
        public Model.Sentinel.RoleResult Role() => Call<object, Model.Sentinel.RoleResult>("ROLE", rt => rt.NewValueToRole().ThrowOrValue());

        public MasterResult[] Masters() => Call<object, MasterResult[]>("SENTINEL".SubCommand("MASTERS"), rt => rt.NewValue(a =>
        {
            var objs = a as object[];
            return objs.Select(x => x.ConvertTo<string[]>().MapToClass<MasterResult>(rt.Encoding)).ToArray();
        }).ThrowOrValue());
        public MasterResult Master(string masterName) => Call<string[], MasterResult>("SENTINEL".SubCommand("MASTER").InputRaw(masterName),
            rt => rt.NewValue(a => a.MapToClass<MasterResult>(rt.Encoding)).ThrowOrValue());

        public SalveResult[] Salves(string masterName) => Call<object, SalveResult[]>("SENTINEL".SubCommand("SLAVES").InputRaw(masterName), rt => rt.NewValue(a =>
        {
            var objs = a as object[];
            return objs.Select(x => x.ConvertTo<string[]>().MapToClass<SalveResult>(rt.Encoding)).ToArray();
        }).ThrowOrValue());
        public SentinelResult[] Sentinels(string masterName) => Call<object, SentinelResult[]>("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName), rt => rt.NewValue(a =>
        {
            var objs = a as object[];
            return objs.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelResult>(rt.Encoding)).ToArray();
        }).ThrowOrValue());
        public string GetMasterAddrByName(string masterName) => Call<string[], string>("SENTINEL".SubCommand("GET-MASTER-ADDR-BY-NAME").InputRaw(masterName), rt => rt.NewValue(a => $"{a[0]}:{a[1]}").ThrowOrValue());
        public IsMaterDownByAddrResult IsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call<string[], IsMaterDownByAddrResult>("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid),
            rt => rt.NewValue(a => new IsMaterDownByAddrResult { down_state = a[0].ConvertTo<bool>(), leader = a[1], vote_epoch = a[1].ConvertTo<long>() }).ThrowOrValue());

        public long Reset(string pattern) => Call<long>("SENTINEL".SubCommand("RESET").InputRaw(pattern), rt => rt.ThrowOrValue());
        public void Failover(string masterName) => Call<object>("SENTINEL".SubCommand("FAILOVER").InputRaw(masterName), rt => rt.ThrowOrValue());



        public object PendingScripts() => Call<object>("SENTINEL".SubCommand("PENDING-SCRIPTS"), rt => rt.ThrowOrValue());
        public object Monitor(string name, string ip, int port, int quorum) => Call<object>("SENTINEL".SubCommand("MONITOR").Input(name, ip, port, quorum), rt => rt.ThrowOrValue());



        public void FlushConfig() => Call<object>("SENTINEL".SubCommand("FLUSHCONFIG"), rt => rt.ThrowOrValue());
        public void Remove(string masterName) => Call<object>("SENTINEL".SubCommand("REMOVE").InputRaw(masterName), rt => rt.ThrowOrValue());
        public string CkQuorum(string masterName) => Call<string>("SENTINEL".SubCommand("CKQUORUM").InputRaw(masterName), rt => rt.ThrowOrValue());
        public void Set(string masterName, string option, string value) => Call<object>("SENTINEL".SubCommand("SET").Input(masterName, option, value), rt => rt.ThrowOrValue());



        public object InfoCache(string masterName) => Call<object>("SENTINEL".SubCommand("INFO-CACHE").InputRaw(masterName), rt => rt.ThrowOrValue());
        public void SimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL"
            .SubCommand("SIMULATE-FAILURE")
            .InputIf(crashAfterElection, "crash-after-election")
            .InputIf(crashAfterPromotion, "crash-after-promotion"), rt => rt.ThrowOrValue());
    }
}

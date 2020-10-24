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

        //~RedisSentinelClient() => this.Dispose();
        int _disposeCounter;
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCounter) != 1) return;
            _redisSocket.Dispose();
        }


        protected TValue Call<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
        {
            _redisSocket.Write(cmd);
            var result = cmd.Read<string>();
            return parse(result);
        }

        public string Ping() => Call("PING", rt => rt.ThrowOrValue<string>());
        public InfoResult Info() => Call("INFO", rt => rt.ThrowOrValue(a => new InfoResult(a.ConvertTo<string>())));
        public Model.Sentinel.RoleResult Role() => Call("ROLE", rt => rt.ThrowOrValueToRole());

        public MasterResult[] Masters() => Call("SENTINEL".SubCommand("MASTERS"), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<MasterResult>(rt.Encoding)).ToArray()));
        public MasterResult Master(string masterName) => Call("SENTINEL".SubCommand("MASTER").InputRaw(masterName), rt => rt.ThrowOrValue(a => 
            a.ConvertTo<string[]>().MapToClass<MasterResult>(rt.Encoding)));

        public SalveResult[] Salves(string masterName) => Call("SENTINEL".SubCommand("SLAVES").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<SalveResult>(rt.Encoding)).ToArray()));
        public SentinelResult[] Sentinels(string masterName) => Call("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelResult>(rt.Encoding)).ToArray()));
        public string GetMasterAddrByName(string masterName) => Call("SENTINEL".SubCommand("GET-MASTER-ADDR-BY-NAME").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) => 
            $"{a[0]}:{a[1]}"));
        public IsMaterDownByAddrResult IsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid), rt => rt.ThrowOrValue((a, _) => 
            new IsMaterDownByAddrResult { down_state = a[0].ConvertTo<bool>(), leader = a[1].ConvertTo<string>(), vote_epoch = a[1].ConvertTo<long>() }));

        public long Reset(string pattern) => Call("SENTINEL".SubCommand("RESET").InputRaw(pattern), rt => rt.ThrowOrValue<long>());
        public void Failover(string masterName) => Call("SENTINEL".SubCommand("FAILOVER").InputRaw(masterName), rt => rt.ThrowOrNothing());



        public object PendingScripts() => Call("SENTINEL".SubCommand("PENDING-SCRIPTS"), rt => rt.ThrowOrValue());
        public object Monitor(string name, string ip, int port, int quorum) => Call("SENTINEL".SubCommand("MONITOR").Input(name, ip, port, quorum), rt => rt.ThrowOrValue());



        public void FlushConfig() => Call("SENTINEL".SubCommand("FLUSHCONFIG"), rt => rt.ThrowOrNothing());
        public void Remove(string masterName) => Call("SENTINEL".SubCommand("REMOVE").InputRaw(masterName), rt => rt.ThrowOrNothing());
        public string CkQuorum(string masterName) => Call("SENTINEL".SubCommand("CKQUORUM").InputRaw(masterName), rt => rt.ThrowOrValue<string>());
        public void Set(string masterName, string option, string value) => Call("SENTINEL".SubCommand("SET").Input(masterName, option, value), rt => rt.ThrowOrNothing());



        public object InfoCache(string masterName) => Call<object>("SENTINEL".SubCommand("INFO-CACHE").InputRaw(masterName), rt => rt.ThrowOrValue());
        public void SimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL"
            .SubCommand("SIMULATE-FAILURE")
            .InputIf(crashAfterElection, "crash-after-election")
            .InputIf(crashAfterPromotion, "crash-after-promotion"), rt => rt.ThrowOrNothing());
    }
}

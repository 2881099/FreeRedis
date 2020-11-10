using FreeRedis.Internal;
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

        public RedisSentinelClient(ConnectionStringBuilder connectionString)
        {
            _redisSocket = new DefaultRedisSocket(connectionString.Host, connectionString.Ssl);
            _redisSocket.ReceiveTimeout = connectionString.ReceiveTimeout;
            _redisSocket.SendTimeout = connectionString.SendTimeout;
            _redisSocket.Encoding = connectionString.Encoding;
            _redisSocket.Connected += (_, __) =>
            {
                if (!string.IsNullOrEmpty(connectionString.User) && !string.IsNullOrEmpty(connectionString.Password))
                    this.Auth(connectionString.User, connectionString.Password);
                else if (!string.IsNullOrEmpty(connectionString.Password))
                    this.Auth(connectionString.Password);
            };
        }

        public void Dispose()
        {
            _redisSocket.Dispose();
        }

        public void Auth(string password) => Call("AUTH".Input(password), rt => rt.ThrowOrValue());
        public void Auth(string username, string password) => Call("AUTH".SubCommand(null)
            .InputIf(!string.IsNullOrWhiteSpace(username), username)
            .InputRaw(password), rt => rt.ThrowOrValue());

        public object Call(CommandPacket cmd) => Call(cmd, rt => rt.ThrowOrValue());
        protected TValue Call<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
        {
            _redisSocket.Write(cmd);
            var rt = _redisSocket.Read(cmd);
            return parse(rt);
        }

        public string Ping() => Call("PING", rt => rt.ThrowOrValue<string>());
        public string Info() => Call("INFO", rt => rt.ThrowOrValue<string>());
        public SentinelRoleResult Role() => Call("ROLE", rt => rt.ThrowOrValueToRole());

        public SentinelMasterResult[] Masters() => Call("SENTINEL".SubCommand("MASTERS"), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelMasterResult>(rt.Encoding)).ToArray()));
        public SentinelMasterResult Master(string masterName) => Call("SENTINEL".SubCommand("MASTER").InputRaw(masterName), rt => rt.ThrowOrValue(a => 
            a.ConvertTo<string[]>().MapToClass<SentinelMasterResult>(rt.Encoding)));

        public SentinelSalveResult[] Salves(string masterName) => Call("SENTINEL".SubCommand("SLAVES").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelSalveResult>(rt.Encoding)).ToArray()));
        public SentinelResult[] Sentinels(string masterName) => Call("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) =>
            a.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelResult>(rt.Encoding)).ToArray()));
        public string GetMasterAddrByName(string masterName) => Call("SENTINEL".SubCommand("GET-MASTER-ADDR-BY-NAME").InputRaw(masterName), rt => rt.ThrowOrValue((a, _) => 
            $"{a[0]}:{a[1]}"));
        public SentinelIsMaterDownByAddrResult IsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid), rt => rt.ThrowOrValue((a, _) => 
            new SentinelIsMaterDownByAddrResult { down_state = a[0].ConvertTo<bool>(), leader = a[1].ConvertTo<string>(), vote_epoch = a[1].ConvertTo<long>() }));

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

    #region Model
    public class SentinelRoleResult
    {
        public static implicit operator SentinelRoleResult(RoleResult rt) => new SentinelRoleResult { role = rt.role, masters = rt.data as string[] };

        public RoleType role;
        public string[] masters;
    }

    // 1) "name"
    // 2) "mymaster"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "6381"
    // 7) "runid"
    // 8) "380dc0424db52c1ff2d1c094659284de55be10fb"
    // 9) "flags"
    //10) "master"
    //11) "link-pending-commands"
    //12) "0"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "0"
    //17) "last-ok-ping-reply"
    //18) "755"
    //19) "last-ping-reply"
    //20) "755"
    //21) "down-after-milliseconds"
    //22) "5000"
    //23) "info-refresh"
    //24) "5375"
    //25) "role-reported"
    //26) "master"
    //27) "role-reported-time"
    //28) "55603"
    //29) "config-epoch"
    //30) "304"
    //31) "num-slaves"
    //32) "2"
    //33) "num-other-sentinels"
    //34) "3"
    //35) "quorum"
    //36) "2"
    //37) "failover-timeout"
    //38) "15000"
    //39) "parallel-syncs"
    //40) "1"
    public class SentinelMasterResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long down_after_milliseconds;
        public long info_refresh;
        public string role_reported;
        public long role_reported_time;
        public long config_epoch;
        public long num_slaves;
        public long num_other_sentinels;
        public long quorum;
        public long failover_timeout;
        public long parallel_syncs;
    }

    // 1) "name"
    // 2) "127.0.0.1:6379"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "6379"
    // 7) "runid"
    // 8) ""
    // 9) "flags"
    //10) "s_down,slave"
    //11) "link-pending-commands"
    //12) "100"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "11188943"
    //17) "last-ok-ping-reply"
    //18) "11188943"
    //19) "last-ping-reply"
    //20) "11188943"
    //21) "s-down-time"
    //22) "11183890"
    //23) "down-after-milliseconds"
    //24) "5000"
    //25) "info-refresh"
    //26) "1603036921117"
    //27) "role-reported"
    //28) "slave"
    //29) "role-reported-time"
    //30) "11188943"
    //31) "master-link-down-time"
    //32) "0"
    //33) "master-link-status"
    //34) "err"
    //35) "master-host"
    //36) "?"
    //37) "master-port"
    //38) "0"
    //39) "slave-priority"
    //40) "100"
    //41) "slave-repl-offset"
    //42) "0"
    public class SentinelSalveResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long s_down_time;
        public long down_after_milliseconds;
        public long info_refresh;
        public string role_reported;
        public long role_reported_time;
        public long master_link_down_time;
        public string master_link_status;
        public string master_host;
        public int master_port;
        public long slave_priority;
        public long slave_repl_offset;
    }

    // 1) "name"
    // 2) "311f72064b0a58ee7f9d49dab078dada24a2b95c"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "26479"
    // 7) "runid"
    // 8) "311f72064b0a58ee7f9d49dab078dada24a2b95c"
    // 9) "flags"
    //10) "sentinel"
    //11) "link-pending-commands"
    //12) "0"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "0"
    //17) "last-ok-ping-reply"
    //18) "364"
    //19) "last-ping-reply"
    //20) "364"
    //21) "down-after-milliseconds"
    //22) "5000"
    //23) "last-hello-message"
    //24) "325"
    //25) "voted-leader"
    //26) "?"
    //27) "voted-leader-epoch"
    //28) "0"
    public class SentinelResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long pending_commands;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long down_after_milliseconds;
        public long last_hello_message;
        public string voted_leader;
        public long voted_leader_epoch;
    }

    public class SentinelIsMaterDownByAddrResult
    {
        public bool down_state;
        public string leader;
        public long vote_epoch;
    }
    #endregion
}

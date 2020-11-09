using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string[] AclCat(string categoryname = null) => Call("ACL"
            .SubCommand("CAT")
            .InputIf(string.IsNullOrWhiteSpace(categoryname) == false, categoryname), rt => rt.ThrowOrValue<string[]>());

        public long AclDelUser(params string[] username) => Call("ACL".SubCommand("DELUSER").Input(username), rt => rt.ThrowOrValue<long>());
        public string AclGenPass(int bits = 0) => Call("ACL"
            .SubCommand("GENPASS")
            .InputIf(bits > 0, bits), rt => rt.ThrowOrValue<string>());

        public AclGetUserResult AclGetUser(string username = "default") => Call("ACL".SubCommand("GETUSER").InputRaw(username), rt => rt.ThrowOrValue((a, _) =>
        {
            a[1] = a[1]?.ConvertTo<string[]>();
            a[3] = a[3]?.ConvertTo<string[]>();
            a[7] = a[7]?.ConvertTo<string[]>();
            return a.MapToClass<AclGetUserResult>(rt.Encoding);
        }));
        public string[] AclList() => Call("ACL".SubCommand("LIST"), rt => rt.ThrowOrValue<string[]>());
        public void AclLoad() => Call("ACL".SubCommand("LOAD"), rt => rt.ThrowOrValue());
        public LogResult[] AclLog(long count = 0) => Call("ACL"
            .SubCommand("LOG")
            .InputIf(count > 0, count), rt => rt
            .ThrowOrValue((a, _) => a.Select(x => x.ConvertTo<object[]>().MapToClass<LogResult>(rt.Encoding)).ToArray()));

        public void AclSave() => Call("ACL".SubCommand("SAVE"), rt => rt.ThrowOrValue());
        public void AclSetUser(string username, params string[] rule) => Call("ACL".SubCommand("SETUSER").InputRaw(username).Input(rule), rt => rt.ThrowOrValue());
        public string[] AclUsers() => Call("ACL".SubCommand("USERS"), rt => rt.ThrowOrValue<string[]>());
        public string AclWhoami() => Call("ACL".SubCommand("WHOAMI"), rt => rt.ThrowOrValue<string>());

        public string BgRewriteAof() => Call("BGREWRITEAOF", rt => rt.ThrowOrValue<string>());
        public string BgSave(string schedule = null) => Call("BGSAVE".SubCommand(null).InputIf(string.IsNullOrEmpty(schedule) == false, schedule), rt => rt.ThrowOrValue<string>());
        public object[] Command() => Call("COMMAND", rt => rt.ThrowOrValue((a, _) => a));
        public long CommandCount() => Call("COMMAND".SubCommand("COUNT"), rt => rt.ThrowOrValue<long>());
        public string[] CommandGetKeys(params string[] command) => Call("COMMAND".SubCommand("GETKEYS").Input(command), rt => rt.ThrowOrValue<string[]>());
        public object[] CommandInfo(params string[] command) => Call("COMMAND".SubCommand("INFO").Input(command), rt => rt.ThrowOrValue<object[]>());

        public Dictionary<string, string> ConfigGet(string parameter) => Call("CONFIG".SubCommand("GET").InputRaw(parameter), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public void ConfigResetStat() => Call("CONFIG".SubCommand("RESETSTAT"), rt => rt.ThrowOrValue());
        public void ConfigRewrite() => Call("CONFIG".SubCommand("REWRITE"), rt => rt.ThrowOrValue());
        public void ConfigSet<T>(string parameter, T value) => Call("CONFIG".SubCommand("SET").InputRaw(parameter).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue());

        public long DbSize() => Call("DBSIZE", rt => rt.ThrowOrValue<long>());
        public string DebugObject(string key) => Call("DEBUG".SubCommand("OBJECT").InputKey(key), rt => rt.ThrowOrValue<string>());
        public void FlushAll(bool isasync = false) => Call("FLUSHALL".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue());
        public void FlushDb(bool isasync = false) => Call("FLUSHDB".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue());
        public string Info(string section = null) => Call("INFO".Input(section), rt => rt.ThrowOrValue<string>());

        public DateTime LastSave() => Call("LASTSAVE", rt => rt.ThrowOrValue(a => _epoch.AddSeconds(a.ConvertTo<long>()).ToLocalTime()));
        public string LatencyDoctor() => Call("LATENCY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        //public string LatencyGraph(string @event) => Call("LATENCY".SubCommand("GRAPH").InputRaw(@event), rt => rt.ThrowOrValue<string>());
        //public object LatencyHistory(string @event) => Call("LATENCY".SubCommand("HISTORY").InputRaw(@event), rt => rt.ThrowOrValue());
        //public object LatencyLatest() => Call("LATENCY".SubCommand("LATEST"), rt => rt.ThrowOrValue());
        //public long LatencyReset(string @event) => Call("LATENCY".SubCommand("RESET").InputRaw(@event), rt => rt.ThrowOrValue<long>());

        //public string Lolwut(string version) => Call("LOLWUT".SubCommand(null).InputIf(string.IsNullOrWhiteSpace(version) == false, "VERSION", version), rt => rt.ThrowOrValue<string>());
        public string MemoryDoctor() => Call("MEMORY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        public string MemoryMallocStats() => Call("MEMORY".SubCommand("MALLOC-STATS"), rt => rt.ThrowOrValue<string>());
        public void MemoryPurge() => Call("MEMORY".SubCommand("PURGE"), rt => rt.ThrowOrValue());
        public Dictionary<string, object> MemoryStats() => Call("MEMORY".SubCommand("STATS"), rt => rt.ThrowOrValue((a, _) => a.MapToHash<object>(rt.Encoding)));
        public long MemoryUsage(string key, long count = 0) => Call("MEMORY"
            .SubCommand("USAGE")
            .InputKey(key)
            .InputIf(count > 0, "SAMPLES", count), rt => rt.ThrowOrValue<long>());

        //public string[][] ModuleList() => Call("MODULE".SubCommand("LIST"), rt => rt.ThrowOrValue<string[][]>());
        //public string ModuleLoad(string path, params string[] args) => Call("MODULE".SubCommand("LOAD").InputRaw(path).InputIf(args?.Any() == true, args), rt => rt.ThrowOrValue<string>());
        //public string ModuleUnload(string name) => Call("MODULE".SubCommand("UNLOAD").InputRaw(name), rt => rt.ThrowOrValue<string>());

        //public RedisClient Monitor(Action<object> onData)
        //{
        //    IRedisSocket rds = null;
        //    rds = CallReadWhile(onData, () => rds.IsConnected, "MONITOR");
        //    return rds.Client;
        //}
        //public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
        public void ReplicaOf(string host, int port) => Call("REPLICAOF".Input(host, port), rt => rt.ThrowOrValue());
        public RoleResult Role() => Call("ROLE", rt => rt.ThrowOrValueToRole());
        public void Save() => Call("SAVE", rt => rt.ThrowOrValue());

        //public void Shutdown(bool save) => Call("SHUTDOWN".Input(save ? "SAVE" : "NOSAVE"), rt => rt.ThrowOrValue());
        public void SlaveOf(string host, int port) => Call("SLAVEOF".Input(host, port), rt => rt.ThrowOrValue());
        public object SlowLog(string subcommand, params string[] argument) => Call("SLOWLOG".SubCommand(subcommand).Input(argument), rt => rt.ThrowOrValue());
        public void SwapDb(int index1, int index2) => Call("SWAPDB".Input(index1, index2), rt => rt.ThrowOrValue());
        //public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
        public DateTime Time() => Call("TIME", rt => rt.ThrowOrValue((a, _) => _epoch.AddSeconds(a[0].ConvertTo<long>()).AddTicks(a[1].ConvertTo<long>() * 10).ToLocalTime()));

        static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    #region Model
    static partial class RedisResultThrowOrValueExtensions
    {
        public static RoleResult ThrowOrValueToRole(this RedisResult rt) =>
            rt.ThrowOrValue((a, _) =>
            {
                if (a?.Any() != true) return null;
                var role = new RoleResult { role = a[0].ConvertTo<RoleType>() };
                switch (role.role)
                {
                    case RoleType.Master:
                        role.data = new RoleResult.MasterInfo
                        {
                            _replication_offset = a[1].ConvertTo<long>(),
                            _slaves = (a[2] as object[])?.Select(x =>
                            {
                                var xs = x as object[];
                                return new RoleResult.MasterInfo.Slave
                                {
                                    ip = xs[0].ConvertTo<string>(),
                                    port = xs[1].ConvertTo<int>(),
                                    slave_offset = xs[2].ConvertTo<long>()
                                };
                            }).ToArray()
                        };
                        break;
                    case RoleType.Slave:
                        role.data = new RoleResult.Slave
                        {
                            master_ip = a[1].ConvertTo<string>(),
                            master_port = a[2].ConvertTo<int>(),
                            replication_state = a[3].ConvertTo<string>(),
                            data_received = a[4].ConvertTo<long>()
                        };
                        break;
                    case RoleType.Sentinel:
                        role.data = a[1].ConvertTo<string[]>();
                        break;
                }
                return role;
            });
    }

    //1) "master"
    //2) (integer) 15891
    //3) 1) 1) "127.0.0.1"
    //      2) "6380"
    //      3) "15617"

    //1) "slave"
    //2) "127.0.0.1"
    //3) (integer) 6381
    //4) "connected"
    //5) (integer) 74933

    //1) "sentinel"
    //2) 1) "mymaster"
    public class RoleResult
    {
        public RoleType role;
        public object data;

        public class MasterInfo
        {
            public long _replication_offset;
            public Slave[] _slaves;

            public override string ToString() => $"{_replication_offset} {string.Join("], [", _slaves.Select(a => a?.ToString()))}";

            public class Slave
            {
                public string ip;
                public int port;
                public long slave_offset;

                public override string ToString() => $"{ip}:{port} {slave_offset}";
            }
        }
        public class Slave
        {
            public string master_ip;
            public int master_port;
            public string replication_state;
            public long data_received;

            public override string ToString() => $"{master_ip}:{master_port} {replication_state} {data_received}";
        }

        public override string ToString() => $"{role}, {data}";
    }

    public class AclGetUserResult
    {
        public string[] flags;
        public string[] passwords;
        public string commands;
        public string[] keys;
    }
    public class LogResult
    {
        public long count;
        public string reason;
        public string context;
        public string @object;
        public string username;
        public decimal age_seconds;
        public string client_info;
    }
    #endregion
}

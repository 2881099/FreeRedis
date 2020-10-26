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
            .SubCommand( "GENPASS")
            .InputIf(bits > 0, bits), rt => rt.ThrowOrValue<string>());

        public object AclGetUser(string username = "default") => Call("ACL".SubCommand("GETUSER").InputRaw(username), rt => rt.ThrowOrValue());
        public object AclHelp() => Call("ACL".SubCommand("HELP"), rt => rt.ThrowOrValue());
        public string[] AclList() => Call("ACL".SubCommand("LIST"), rt => rt.ThrowOrValue<string[]>());
        public string AclLoad() => Call("ACL".SubCommand("LOAD"), rt => rt.ThrowOrValue<string>());
        public LogInfo[] AclLog(long count = 0) => Call("ACL"
            .SubCommand("LOG")
            .InputIf(count > 0, count), rt => rt
            .ThrowOrValue((a, _) => a.Select(x => x.ConvertTo<object[]>().MapToClass<LogInfo>(rt.Encoding)).ToArray()));
        public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }

        public string AclSave() => Call("ACL".SubCommand("SAVE"), rt => rt.ThrowOrValue<string>());
        public string AclSetUser(params string[] rule) => Call("ACL".SubCommand("SETUSER").Input(rule), rt => rt.ThrowOrValue<string>());
        public string[] AclUsers() => Call("ACL".SubCommand("USERS"), rt => rt.ThrowOrValue<string[]>());
        public string AclWhoami() => Call("ACL".SubCommand("WHOAMI"), rt => rt.ThrowOrValue<string>());

        public string BgRewriteAof() => Call("BGREWRITEAOF", rt => rt.ThrowOrValue<string>());
        public string BgSave(string schedule = null) => Call("BGSAVE".SubCommand(null).InputIf(string.IsNullOrEmpty(schedule) == false, schedule), rt => rt.ThrowOrValue<string>());
        public object[] Command() => Call("COMMAND", rt => rt.ThrowOrValue<object[]>());
        public long CommandCount() => Call("COMMAND".SubCommand("COUNT"), rt => rt.ThrowOrValue<long>());
        public string[] CommandGetKeys(params string[] command) => Call<string[]>("COMMAND".SubCommand("GETKEYS").Input(command), rt => rt.ThrowOrValue<string[]>());
        public object[] CommandInfo(params string[] command) => Call("COMMAND".SubCommand("INFO").Input(command), rt => rt.ThrowOrValue<object[]>());

        public Dictionary<string, string> ConfigGet(string parameter) => Call("CONFIG".SubCommand("GET").InputRaw(parameter), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public string ConfigResetStat() => Call("CONFIG".SubCommand("RESETSTAT"), rt => rt.ThrowOrValue<string>());
        public string ConfigRewrite() => Call("CONFIG".SubCommand("REWRITE"), rt => rt.ThrowOrValue<string>());
        public string ConfigSet<T>(string parameter, T value) => Call("CONFIG".SubCommand("SET").InputRaw(parameter).InputRaw(value), rt => rt.ThrowOrValue<string>());

        public long DbSize() => Call("DBSIZE", rt => rt.ThrowOrValue<long>());
        public string DebugObject(string key) => Call("DEBUG".SubCommand("OBJECT").InputRaw(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string DebugSegfault() => Call("DEBUG".SubCommand("SEGFAULT"), rt => rt.ThrowOrValue<string>());
        public string FlushAll(bool isasync = false) => Call("FLUSHALL".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue<string>());
        public string FlushDb(bool isasync = false) => Call("FLUSHDB".SubCommand(null).InputIf(isasync, "ASYNC"), rt => rt.ThrowOrValue<string>());
        public string Info(string section = null) => Call("INFO".Input(section), rt => rt.ThrowOrValue<string>());

        public long LastSave() => Call("LASTSAVE", rt => rt.ThrowOrValue<long>());
        public string LatencyDoctor() => Call("LATENCY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        public string LatencyGraph(string @event) => Call("LATENCY".SubCommand("GRAPH").InputRaw(@event), rt => rt.ThrowOrValue<string>());
        public string[] LatencyHelp() => Call("LATENCY".SubCommand("HELP"), rt => rt.ThrowOrValue<string[]>());
        public string[][] LatencyHistory(string @event) => Call("LATENCY".SubCommand("HISTORY").InputRaw(@event), rt => rt.ThrowOrValue<string[][]>());
        public string[][] LatencyLatest() => Call("LATENCY".SubCommand("LATEST"), rt => rt.ThrowOrValue<string[][]>());
        public long LatencyReset(string @event) => Call("LATENCY".SubCommand("RESET").InputRaw(@event), rt => rt.ThrowOrValue<long>());

        public string Lolwut(string version) => Call("LOLWUT".SubCommand(null).InputIf(string.IsNullOrWhiteSpace(version) == false, "VERSION", version), rt => rt.ThrowOrValue<string>());
        public string MemoryDoctor() => Call("MEMORY".SubCommand("DOCTOR"), rt => rt.ThrowOrValue<string>());
        public string[] MemoryHelp() => Call("MEMORY".SubCommand("HELP"), rt => rt.ThrowOrValue<string[]>());
        public string MemoryMallocStats() => Call("MEMORY".SubCommand("MALLOC-STATS"), rt => rt.ThrowOrValue<string>());
        public string MemoryPurge() => Call("MEMORY".SubCommand("PURGE"), rt => rt.ThrowOrValue<string>());
        public Dictionary<string, string> MemoryStats() => Call("MEMORY".SubCommand("STATS"), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public long MemoryUsage(string key, long count = 0) => Call("MEMORY"
            .SubCommand( "USAGE")
            .InputRaw(key)
            .InputIf(count > 0, "SAMPLES", count)
            .FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[][] ModuleList() => Call("MODULE".SubCommand("LIST"), rt => rt.ThrowOrValue<string[][]>());
        public string ModuleLoad(string path, params string[] args) => Call("MODULE".SubCommand( "LOAD").InputRaw(path).InputIf(args?.Any() == true, args), rt => rt.ThrowOrValue<string>());
        public string ModuleUnload(string name) => Call("MODULE".SubCommand("UNLOAD").InputRaw(name), rt => rt.ThrowOrValue<string>());

        //public RedisClient Monitor(Action<object> onData)
        //{
        //    IRedisSocket rds = null;
        //    rds = CallReadWhile(onData, () => rds.IsConnected, "MONITOR");
        //    return rds.Client;
        //}
        //public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
        public string ReplicaOf(string host, int port) => Call("REPLICAOF".Input(host, port), rt => rt.ThrowOrValue<string>());
        public RoleResult Role() => Call("ROLE", rt => rt.ThrowOrValueToRole());
        public string Save() => Call("SAVE", rt => rt.ThrowOrValue<string>());

        public string Shutdown(bool save) => Call("SHUTDOWN".Input(save ? "SAVE" : "NOSAVE"), rt => rt.ThrowOrValue<string>());
        public string SlaveOf(string host, int port) => Call("SLAVEOF".Input(host, port), rt => rt.ThrowOrValue<string>());
        public object SlowLog(string subcommand, params string[] argument) => Call("SLOWLOG".SubCommand(subcommand).Input(argument), rt => rt.ThrowOrValue());
        public string SwapDb(int index1, int index2) => Call("SWAPDB".Input(index1, index2), rt => rt.ThrowOrValue<string>());
        //public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
        public DateTime Time() => Call("TIME", rt => rt.ThrowOrValue((a, _) => new DateTime(1970, 0, 0).AddSeconds(a[0].ConvertTo<long>()).AddTicks(a[1].ConvertTo<long>() * 10)));
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
    #endregion
}

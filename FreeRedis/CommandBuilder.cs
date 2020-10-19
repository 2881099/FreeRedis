using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    public class CommandBuilder
    {
        public string _command { get; private set; }
        public string _subcommand { get; private set; }
        public List<object> _input { get; } = new List<object>();
        public List<string> _flagKey { get; } = new List<string>();

        public static implicit operator List<object>(CommandBuilder cb) => cb._input;
        public static implicit operator CommandBuilder(string cmd) => new CommandBuilder().Command(cmd);

        public CommandBuilder Command(string cmd, string subcmd = null)
        {
            if (!string.IsNullOrWhiteSpace(_command) && _command.Equals(_input.FirstOrDefault())) _input.RemoveAt(0);
            if (!string.IsNullOrWhiteSpace(_subcommand) && _subcommand.Equals(_input.FirstOrDefault())) _input.RemoveAt(0);

            _command = cmd;
            _subcommand = subcmd;

            if (!string.IsNullOrWhiteSpace(_command))
            {
                if (!string.IsNullOrWhiteSpace(_subcommand)) _input.Insert(0, _subcommand);
                _input.Insert(0, _command);
            }
            return this;
        }
        public CommandBuilder FlagKey(params string[] keys)
        {
            if (keys != null)
                foreach (var key in keys) 
                    if (!string.IsNullOrEmpty(key)) 
                        _flagKey.Add(key);
            return this;
        }
        public CommandBuilder FlagKey(IEnumerable<string> keys)
        {
            if (keys != null)
                foreach (var key in keys)
                    if (!string.IsNullOrEmpty(key))
                        _flagKey.Add(key);
            return this;
        }

        public CommandBuilder InputRaw(object arg)
        {
            _input.Add(arg);
            return this;
        }
        public CommandBuilder Input(string[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandBuilder Input(int[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandBuilder Input(long[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandBuilder Input(params object[] args) => this.InputIf(true, args);
        public CommandBuilder InputIf(bool condition, params object[] args)
        {
            if (condition && args != null)
            {
                foreach (var item in args)
                {
                    if (item is object[] objs) _input.AddRange(objs);
                    else if (item is string[] strs) _input.AddRange(strs.Select(a => (object)a));
                    else if (item is int[] ints) _input.AddRange(ints.Select(a => (object)a));
                    else if (item is long[] longs) _input.AddRange(longs.Select(a => (object)a));
                    else if (item is KeyValuePair<string, long>[] kvps1) _input.AddRange(kvps1.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
                    else if (item is KeyValuePair<string, string>[] kvps2) _input.AddRange(kvps2.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
                    else if (item is Dictionary<string, long> dict1) _input.AddRange(dict1.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
                    else if (item is Dictionary<string, string> dict2) _input.AddRange(dict2.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
                    else _input.Add(item);
                }
            }
            return this;
        }

        public CommandBuilder InputKv(KeyValuePair<string, long>[] args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandBuilder InputKv(KeyValuePair<string, string>[] args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandBuilder InputKv(KeyValuePair<string, object>[] args, Func<object, object> serialize)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, serialize(a.Value) }).SelectMany(a => a).ToArray());
            return this;
        }

        public CommandBuilder InputKv(Dictionary<string, long> args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandBuilder InputKv(Dictionary<string, string> args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandBuilder InputKv(Dictionary<string, object> args, Func<object, object> serialize)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, serialize(a.Value) }).SelectMany(a => a).ToArray());
            return this;
        }
    }

    static class CommandBuilderExtensions
    {
        public static CommandBuilder SubCommand(this string that, string subcmd) => new CommandBuilder().Command(that, subcmd);
        public static CommandBuilder Input(this string that, string arg) => new CommandBuilder().Command(that).InputRaw(arg);
        public static CommandBuilder Input(this string that, long arg) => new CommandBuilder().Command(that).InputRaw(arg);
        public static CommandBuilder Input(this string that, string arg1, string arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, long arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, int arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, int arg1, int arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, long arg1, long arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, string arg2, string arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, string arg2, long arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, string arg2, decimal arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, long arg2, long arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2, string arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2, decimal arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, long arg2, long arg3, long arg4, decimal arg5) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3).InputRaw(arg4).InputRaw(arg5);
        public static CommandBuilder Input(this string that, string[] args) => new CommandBuilder().Command(that).Input(args);
    }

    public class CommandConfig
    {
        public CommandFlag Flag { get; }
        public CommandTag Tag { get; }
        public bool CheckMaxPoolSizeEqualOne { get; }

        public CommandConfig(CommandFlag flag, CommandTag tag)
        {
            this.Flag = flag;
            this.Tag = tag;
        }

        //public static List<string> _allCommands { get; }
        static ConcurrentDictionary<string, CommandConfig> _dicOptions { get; }
        public static CommandConfig Get(string command) => _dicOptions.TryGetValue(command?.ToUpper().Trim(), out var options) ? options : null;
        public static void Register(string command, CommandConfig options)
        {
            command = command?.ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException(nameof(command));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (_dicOptions.TryAdd(command, options) == false) throw new Exception($"The command \"{command}\" is already registered");
        }

        static CommandConfig()
        {
            #region _allCommands
            //_allCommands = new List<string>
            //{
            //    "BF.RESERVE",
            //    "BF.ADD",
            //    "BF.MADD",
            //    "BF.INSERT",
            //    "BF.EXISTS",
            //    "BF.MEXISTS",
            //    "BF.SCANDUMP",
            //    "BF.LOADCHUNK",
            //    "BF.INFO",

            //    "CMS.INITBYDIM",
            //    "CMS.INITBYPROB",
            //    "CMS.INCRBY",
            //    "CMS.INCRBY",
            //    "CMS.QUERY",
            //    "CMS.MERGE",
            //    "CMS.INFO",

            //    "CF.RESERVE",
            //    "CF.ADD",
            //    "CF.ADDNX",
            //    "CF.INSERT",
            //    "CF.INSERTNX",
            //    "CF.EXISTS",
            //    "CF.DEL",
            //    "CF.COUNT",
            //    "CF.SCANDUMP",
            //    "CF.LOADCHUNK",
            //    "CF.INFO",

            //    "TOPK.RESERVE",
            //    "TOPK.ADD",
            //    "TOPK.INCRBY",
            //    "TOPK.QUERY",
            //    "TOPK.COUNT",
            //    "TOPK.LIST",
            //    "TOPK.INFO",

            //    "CLUSTER ADDSLOTS",
            //    "CLUSTER BUMPEPOCH",
            //    "CLUSTER COUNT-FAILURE-REPORTS",
            //    "CLUSTER COUNTKEYSINSLOT",
            //    "CLUSTER DELSLOTS",
            //    "CLUSTER FAILOVER",
            //    "CLUSTER FLUSHSLOTS",
            //    "CLUSTER FORGET",
            //    "CLUSTER GETKEYSINSLOT",
            //    "CLUSTER INFO",
            //    "CLUSTER KEYSLOT",
            //    "CLUSTER MEET",
            //    "CLUSTER MYID",
            //    "CLUSTER NODES",
            //    "CLUSTER REPLICAS",
            //    "CLUSTER REPLICATE",
            //    "CLUSTER RESET",
            //    "CLUSTER SAVECONFIG",
            //    "CLUSTER SET-CONFIG-EPOCH",
            //    "CLUSTER SLAVES",
            //    "CLUSTER SLOTS",
            //    "READONLY",
            //    "READWRITE",

            //    "AUTH",
            //    "CLIENT CACHING",
            //    "CLIENT GETNAME",
            //    "CLIENT GETREDIR",
            //    "CLIENT ID",
            //    "CLIENT KILL",
            //    "CLIENT LIST",
            //    "CLIENT PAUSE",
            //    "CLIENT REPLY",
            //    "CLIENT SETNAME",
            //    "CLIENT TRACKING",
            //    "CLIENT UNBLOCK",
            //    "ECHO",
            //    "HELLO ",
            //    "PING ",
            //    "QUIT",
            //    "SELECT",

            //    "GEOADD",
            //    "GEODIST",
            //    "GEOHASH",
            //    "GEOPOS",
            //    "GEORADIUS",
            //    "GEORADIUSBYMEMBER",

            //    "HDEL",
            //    "HEXISTS",
            //    "HGET",
            //    "HGETALL",
            //    "HINCRBY",
            //    "HINCRBYFLOAT",
            //    "HKEYS",
            //    "HLEN",
            //    "HMGET",
            //    "HMSET",
            //    "HSCAN",
            //    "HSET",
            //    "HSETNX",
            //    "HSTRLEN",
            //    "HVALS",

            //    "PFADD",
            //    "PFCOUNT",
            //    "PFMERGE",

            //    "DEL",
            //    "DUMP",
            //    "EXISTS",
            //    "EXPIRE",
            //    "EXPIREAT",
            //    "KEYS",
            //    "MIGRATE",
            //    "MOVE",
            //    "OBJECT REFCOUNT",
            //    "OBJECT IDLETIME",
            //    "OBJECT ENCODING",
            //    "OBJECT FREQ",
            //    "OBJECT HELP",
            //    "PERSIST",
            //    "PEXPIRE",
            //    "PEXPIREAT",
            //    "PTTL",
            //    "RANDOMKEY",
            //    "RENAME",
            //    "RENAMENX",
            //    "RESTORE",
            //    "SCAN",
            //    "SORT",
            //    "TOUCH",
            //    "TTL",
            //    "TYPE",
            //    "UNLINK",
            //    "WAIT",

            //    "BLPOP",
            //    "BRPOP",
            //    "BRPOPLPUSH",
            //    "LINDEX",
            //    "LINSERT",
            //    "LLEN",
            //    "LPOP",
            //    "LPOS",
            //    "LPUSH",
            //    "LPUSHX",
            //    "LRANGE",
            //    "LREM",
            //    "LSET",
            //    "LTRIM",
            //    "RPOP",
            //    "RPOPLPUSH",
            //    "RPUSH",
            //    "RPUSHX",

            //    "EVAL",
            //    "EVALSHA",
            //    "SCRIPT DEBUG",
            //    "SCRIPT EXISTS",
            //    "SCRIPT FLUSH",
            //    "SCRIPT KILL",
            //    "SCRIPT LOAD",

            //    "ACL CAT",
            //    "ACL DELUSER",
            //    "ACL GENPASS",
            //    "ACL GETUSER",
            //    "ACL HELP",
            //    "ACL LIST",
            //    "ACL LOAD",
            //    "ACL LOG",
            //    "ACL SAVE",
            //    "ACL SETUSER",
            //    "ACL USERS",
            //    "ACL WHOAMI",
            //    "BGREWRITEAOF",
            //    "BGSAVE",
            //    "COMMAND",
            //    "COMMAND COUNT",
            //    "COMMAND GETKEYS",
            //    "COMMAND INFO",
            //    "CONFIG GET",
            //    "CONFIG RESETSTAT",
            //    "CONFIG REWRITE",
            //    "CONFIG SET",
            //    "DBSIZE",
            //    "DEBUG OBJECT",
            //    "DEBUG SEGFAULT",
            //    "FLUSHALL",
            //    "FLUSHDB",
            //    "INFO",
            //    "LASTSAVE",
            //    "LATENCY DOCTOR",
            //    "LATENCY GRAPH",
            //    "LATENCY HELP",
            //    "LATENCY HISTORY",
            //    "LATENCY LATEST",
            //    "LATENCY RESET",
            //    "LOLWUT",
            //    "MEMORY DOCTOR",
            //    "MEMORY HELP",
            //    "MEMORY MALLOC-STATS",
            //    "MEMORY PURGE",
            //    "MEMORY STATS",
            //    "MEMORY USAGE",
            //    "MODULE LIST",
            //    "MODULE LOAD",
            //    "MODULE UNLOAD",
            //    "MONITOR",
            //    "REPLICAOF",
            //    "ROLE",
            //    "SAVE",
            //    "SHUTDOWN",
            //    "SLOWLOG",
            //    "SWAPDB",
            //    "SYNC",
            //    "TIME",

            //    "SADD",
            //    "SCARD",
            //    "SDIFF",
            //    "SDIFFSTORE",
            //    "SINTER",
            //    "SINTERSTORE",
            //    "SISMEMBER",
            //    "SMEMBERS",
            //    "SMISMEMBER",
            //    "SMOVE",
            //    "SPOP",
            //    "SRANDMEMBER",
            //    "SREM",
            //    "SSCAN",
            //    "SUNION",
            //    "SUNIONSTORE",

            //    "BZPOPMAX",
            //    "BZPOPMIN",
            //    "ZADD",
            //    "ZCARD",
            //    "ZCOUNT",
            //    "ZINCRBY",
            //    "ZINTERSTORE",
            //    "ZLEXCOUNT",
            //    "ZPOPMAX",
            //    "ZPOPMIN",
            //    "ZRANGE",
            //    "ZRANGEBYLEX",
            //    "ZRANGEBYSCORE",
            //    "ZRANK",
            //    "ZREM",
            //    "ZREMRANGEBYLEX",
            //    "ZREMRANGEBYRANK",
            //    "ZREMRANGEBYSCORE",
            //    "ZREVRANGE",
            //    "ZREVRANGEBYLEX",
            //    "ZREVRANGEBYSCORE",
            //    "ZREVRANK",
            //    "ZSCAN",
            //    "ZSCORE",

            //    "XACK",
            //    "XADD",
            //    "XCLAIM",
            //    "XDEL",
            //    "XGROUP CREATE",
            //    "XGROUP SETID",
            //    "XGROUP DESTROY",
            //    "XGROUP DELCONSUMER",
            //    "XINFO STREAM",
            //    "XINFO GROUPS",
            //    "XINFO CONSUMERS",
            //    "XLEN",
            //    "XPENDING",
            //    "XRANGE",
            //    "XREVRANGE",
            //    "XREAD",
            //    "XREADGROUP",
            //    "XTRIM",

            //    "APPEND",
            //    "BITCOUNT",
            //    "BITFIELD",
            //    "BITOP",
            //    "BITPOS",
            //    "DECR",
            //    "DECRBY",
            //    "GET",
            //    "GETBIT",
            //    "GETRANGE",
            //    "GETSET",
            //    "INCR",
            //    "INCRBY",
            //    "INCRBYFLOAT",
            //    "MGET",
            //    "MSET",
            //    "MSETNX",
            //    "PSETEX",
            //    "SET",
            //    "SETBIT",
            //    "SETEX",
            //    "SETNX",
            //    "SETRANGE",
            //    "STRALGO",
            //    "STRLEN",

            //    "DISCARD",
            //    "EXEC",
            //    "MULTI",
            //    "UNWATCH",
            //    "WATCH",
            //};
            #endregion
            _dicOptions = new ConcurrentDictionary<string, CommandConfig>
            {
                ["BF.RESERVE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.ADD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.MADD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.INSERT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.EXISTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.MEXISTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.SCANDUMP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.LOADCHUNK"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BF.INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.INITBYDIM"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.INITBYPROB"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.INCRBY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.INCRBY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.QUERY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.MERGE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CMS.INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.RESERVE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.ADD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.ADDNX"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.INSERT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.INSERTNX"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.EXISTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.DEL"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.COUNT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.SCANDUMP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.LOADCHUNK"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CF.INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.RESERVE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.ADD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.INCRBY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.QUERY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.COUNT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.LIST"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["TOPK.INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER ADDSLOTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER BUMPEPOCH"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER COUNT-FAILURE-REPORTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER COUNTKEYSINSLOT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER DELSLOTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER FAILOVER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER FLUSHSLOTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER FORGET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER GETKEYSINSLOT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER KEYSLOT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER MEET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER MYID"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER NODES"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER REPLICAS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER REPLICATE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER RESET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER SAVECONFIG"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER SET-CONFIG-EPOCH"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER SLAVES"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLUSTER SLOTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["READONLY"] = new CommandConfig(CommandFlag.fast, CommandTag.keyspace | CommandTag.fast),
                ["READWRITE"] = new CommandConfig(CommandFlag.fast, CommandTag.keyspace | CommandTag.fast),
                ["AUTH"] = new CommandConfig(CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale | CommandFlag.skip_monitor | CommandFlag.skip_slowlog | CommandFlag.fast | CommandFlag.no_auth, CommandTag.fast | CommandTag.connection),
                ["CLIENT CACHING"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT GETNAME"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT GETREDIR"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT ID"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT KILL"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT LIST"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT PAUSE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT REPLY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT SETNAME"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT TRACKING"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CLIENT UNBLOCK"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ECHO"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.fast | CommandTag.connection),
                ["HELLO "] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["PING "] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["QUIT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SELECT"] = new CommandConfig(CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.keyspace | CommandTag.fast),
                ["GEOADD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.geo | CommandTag.slow),
                ["GEODIST"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.geo | CommandTag.slow),
                ["GEOHASH"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.geo | CommandTag.slow),
                ["GEOPOS"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.geo | CommandTag.slow),
                ["GEORADIUS"] = new CommandConfig(CommandFlag.write | CommandFlag.movablekeys, CommandTag.write | CommandTag.geo | CommandTag.slow),
                ["GEORADIUSBYMEMBER"] = new CommandConfig(CommandFlag.write | CommandFlag.movablekeys, CommandTag.write | CommandTag.geo | CommandTag.slow),
                ["HDEL"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HEXISTS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.hash | CommandTag.fast),
                ["HGET"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.hash | CommandTag.fast),
                ["HGETALL"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.hash | CommandTag.slow),
                ["HINCRBY"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HINCRBYFLOAT"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HKEYS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.hash | CommandTag.slow),
                ["HLEN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.hash | CommandTag.fast),
                ["HMGET"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.hash | CommandTag.fast),
                ["HMSET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HSCAN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.hash | CommandTag.slow),
                ["HSET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HSETNX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hash | CommandTag.fast),
                ["HSTRLEN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.hash | CommandTag.fast),
                ["HVALS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.hash | CommandTag.slow),
                ["PFADD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.hyperloglog | CommandTag.fast),
                ["PFCOUNT"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.hyperloglog | CommandTag.slow),
                ["PFMERGE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.hyperloglog | CommandTag.slow),
                ["DEL"] = new CommandConfig(CommandFlag.write, CommandTag.keyspace | CommandTag.write | CommandTag.slow),
                ["DUMP"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.keyspace | CommandTag.read | CommandTag.slow),
                ["EXISTS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["EXPIRE"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["EXPIREAT"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["KEYS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.keyspace | CommandTag.read | CommandTag.slow | CommandTag.dangerous),
                ["MIGRATE"] = new CommandConfig(CommandFlag.write | CommandFlag.random | CommandFlag.movablekeys, CommandTag.keyspace | CommandTag.write | CommandTag.slow | CommandTag.dangerous),
                ["MOVE"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["OBJECT REFCOUNT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["OBJECT IDLETIME"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["OBJECT ENCODING"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["OBJECT FREQ"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["OBJECT HELP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["PERSIST"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["PEXPIRE"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["PEXPIREAT"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["PTTL"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["RANDOMKEY"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.keyspace | CommandTag.read | CommandTag.slow),
                ["RENAME"] = new CommandConfig(CommandFlag.write, CommandTag.keyspace | CommandTag.write | CommandTag.slow),
                ["RENAMENX"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["RESTORE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.keyspace | CommandTag.write | CommandTag.slow | CommandTag.dangerous),
                ["SCAN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.keyspace | CommandTag.read | CommandTag.slow),
                ["SORT"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.movablekeys, CommandTag.write | CommandTag.set | CommandTag.sortedset | CommandTag.list | CommandTag.slow | CommandTag.dangerous),
                ["TOUCH"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["TTL"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["TYPE"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["UNLINK"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast),
                ["WAIT"] = new CommandConfig(CommandFlag.noscript, CommandTag.keyspace | CommandTag.slow),
                ["BLPOP"] = new CommandConfig(CommandFlag.write | CommandFlag.noscript, CommandTag.write | CommandTag.list | CommandTag.slow | CommandTag.blocking),
                ["BRPOP"] = new CommandConfig(CommandFlag.write | CommandFlag.noscript, CommandTag.write | CommandTag.list | CommandTag.slow | CommandTag.blocking),
                ["BRPOPLPUSH"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.noscript, CommandTag.write | CommandTag.list | CommandTag.slow | CommandTag.blocking),
                ["LINDEX"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.list | CommandTag.slow),
                ["LINSERT"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.list | CommandTag.slow),
                ["LLEN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.list | CommandTag.fast),
                ["LPOP"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["LPOS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LPUSH"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["LPUSHX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["LRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.list | CommandTag.slow),
                ["LREM"] = new CommandConfig(CommandFlag.write, CommandTag.write | CommandTag.list | CommandTag.slow),
                ["LSET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.list | CommandTag.slow),
                ["LTRIM"] = new CommandConfig(CommandFlag.write, CommandTag.write | CommandTag.list | CommandTag.slow),
                ["RPOP"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["RPOPLPUSH"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.list | CommandTag.slow),
                ["RPUSH"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["RPUSHX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.list | CommandTag.fast),
                ["EVAL"] = new CommandConfig(CommandFlag.noscript | CommandFlag.movablekeys, CommandTag.slow | CommandTag.scripting),
                ["EVALSHA"] = new CommandConfig(CommandFlag.noscript | CommandFlag.movablekeys, CommandTag.slow | CommandTag.scripting),
                ["SCRIPT DEBUG"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SCRIPT EXISTS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SCRIPT FLUSH"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SCRIPT KILL"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SCRIPT LOAD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL CAT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL DELUSER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL GENPASS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL GETUSER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL HELP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL LIST"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL LOAD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL LOG"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL SAVE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL SETUSER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL USERS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["ACL WHOAMI"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["BGREWRITEAOF"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["BGSAVE"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["COMMAND"] = new CommandConfig(CommandFlag.random | CommandFlag.loading | CommandFlag.stale, CommandTag.slow | CommandTag.connection),
                ["COMMAND COUNT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["COMMAND GETKEYS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["COMMAND INFO"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CONFIG GET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CONFIG RESETSTAT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CONFIG REWRITE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["CONFIG SET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["DBSIZE"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.keyspace | CommandTag.read | CommandTag.fast),
                ["DEBUG OBJECT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["DEBUG SEGFAULT"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["FLUSHALL"] = new CommandConfig(CommandFlag.write, CommandTag.keyspace | CommandTag.write | CommandTag.slow | CommandTag.dangerous),
                ["FLUSHDB"] = new CommandConfig(CommandFlag.write, CommandTag.keyspace | CommandTag.write | CommandTag.slow | CommandTag.dangerous),
                ["INFO"] = new CommandConfig(CommandFlag.random | CommandFlag.loading | CommandFlag.stale, CommandTag.slow | CommandTag.dangerous),
                ["LASTSAVE"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random | CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.read | CommandTag.admin | CommandTag.fast | CommandTag.dangerous),
                ["LATENCY DOCTOR"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LATENCY GRAPH"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LATENCY HELP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LATENCY HISTORY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LATENCY LATEST"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LATENCY RESET"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["LOLWUT"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.fast),
                ["MEMORY DOCTOR"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MEMORY HELP"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MEMORY MALLOC-STATS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MEMORY PURGE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MEMORY STATS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MEMORY USAGE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MODULE LIST"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MODULE LOAD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MODULE UNLOAD"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["MONITOR"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["REPLICAOF"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript | CommandFlag.stale, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["ROLE"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.read | CommandTag.fast | CommandTag.dangerous),
                ["SAVE"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["SHUTDOWN"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["SLOWLOG"] = new CommandConfig(CommandFlag.admin | CommandFlag.random | CommandFlag.loading | CommandFlag.stale, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["SWAPDB"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.keyspace | CommandTag.write | CommandTag.fast | CommandTag.dangerous),
                ["SYNC"] = new CommandConfig(CommandFlag.admin | CommandFlag.noscript, CommandTag.admin | CommandTag.slow | CommandTag.dangerous),
                ["TIME"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random | CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.read | CommandTag.fast),
                ["SADD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.set | CommandTag.fast),
                ["SCARD"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.set | CommandTag.fast),
                ["SDIFF"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SDIFFSTORE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.set | CommandTag.slow),
                ["SINTER"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SINTERSTORE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.set | CommandTag.slow),
                ["SISMEMBER"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.set | CommandTag.fast),
                ["SMEMBERS"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SMISMEMBER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["SMOVE"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.set | CommandTag.fast),
                ["SPOP"] = new CommandConfig(CommandFlag.write | CommandFlag.random | CommandFlag.fast, CommandTag.write | CommandTag.set | CommandTag.fast),
                ["SRANDMEMBER"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SREM"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.set | CommandTag.fast),
                ["SSCAN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SUNION"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.sort_for_script, CommandTag.read | CommandTag.set | CommandTag.slow),
                ["SUNIONSTORE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.set | CommandTag.slow),
                ["BZPOPMAX"] = new CommandConfig(CommandFlag.write | CommandFlag.noscript | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast | CommandTag.blocking),
                ["BZPOPMIN"] = new CommandConfig(CommandFlag.write | CommandFlag.noscript | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast | CommandTag.blocking),
                ["ZADD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast),
                ["ZCARD"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["ZCOUNT"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["ZINCRBY"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast),
                ["ZINTERSTORE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.movablekeys, CommandTag.write | CommandTag.sortedset | CommandTag.slow),
                ["ZLEXCOUNT"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["ZPOPMAX"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast),
                ["ZPOPMIN"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast),
                ["ZRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZRANGEBYLEX"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZRANGEBYSCORE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZRANK"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["ZREM"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.sortedset | CommandTag.fast),
                ["ZREMRANGEBYLEX"] = new CommandConfig(CommandFlag.write, CommandTag.write | CommandTag.sortedset | CommandTag.slow),
                ["ZREMRANGEBYRANK"] = new CommandConfig(CommandFlag.write, CommandTag.write | CommandTag.sortedset | CommandTag.slow),
                ["ZREMRANGEBYSCORE"] = new CommandConfig(CommandFlag.write, CommandTag.write | CommandTag.sortedset | CommandTag.slow),
                ["ZREVRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZREVRANGEBYLEX"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZREVRANGEBYSCORE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZREVRANK"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["ZSCAN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.sortedset | CommandTag.slow),
                ["ZSCORE"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.sortedset | CommandTag.fast),
                ["XACK"] = new CommandConfig(CommandFlag.write | CommandFlag.random | CommandFlag.fast, CommandTag.write | CommandTag.stream | CommandTag.fast),
                ["XADD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.random | CommandFlag.fast, CommandTag.write | CommandTag.stream | CommandTag.fast),
                ["XCLAIM"] = new CommandConfig(CommandFlag.write | CommandFlag.random | CommandFlag.fast, CommandTag.write | CommandTag.stream | CommandTag.fast),
                ["XDEL"] = new CommandConfig(CommandFlag.write | CommandFlag.fast, CommandTag.write | CommandTag.stream | CommandTag.fast),
                ["XGROUP CREATE"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XGROUP SETID"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XGROUP DESTROY"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XGROUP DELCONSUMER"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XINFO STREAM"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XINFO GROUPS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XINFO CONSUMERS"] = new CommandConfig(CommandFlag.none, CommandTag.none),
                ["XLEN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.stream | CommandTag.fast),
                ["XPENDING"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.random, CommandTag.read | CommandTag.stream | CommandTag.slow),
                ["XRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.stream | CommandTag.slow),
                ["XREVRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.stream | CommandTag.slow),
                ["XREAD"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.movablekeys, CommandTag.read | CommandTag.stream | CommandTag.slow | CommandTag.blocking),
                ["XREADGROUP"] = new CommandConfig(CommandFlag.write | CommandFlag.movablekeys, CommandTag.write | CommandTag.stream | CommandTag.slow | CommandTag.blocking),
                ["XTRIM"] = new CommandConfig(CommandFlag.write | CommandFlag.random, CommandTag.write | CommandTag.stream | CommandTag.slow),
                ["APPEND"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["BITCOUNT"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.bitmap | CommandTag.slow),
                ["BITFIELD"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.bitmap | CommandTag.slow),
                ["BITOP"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.bitmap | CommandTag.slow),
                ["BITPOS"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.bitmap | CommandTag.slow),
                ["DECR"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["DECRBY"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["GET"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.@string | CommandTag.fast),
                ["GETBIT"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.bitmap | CommandTag.fast),
                ["GETRANGE"] = new CommandConfig(CommandFlag.@readonly, CommandTag.read | CommandTag.@string | CommandTag.slow),
                ["GETSET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["INCR"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["INCRBY"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["INCRBYFLOAT"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["MGET"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.@string | CommandTag.fast),
                ["MSET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["MSETNX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["PSETEX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["SET"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["SETBIT"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.bitmap | CommandTag.slow),
                ["SETEX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["SETNX"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom | CommandFlag.fast, CommandTag.write | CommandTag.@string | CommandTag.fast),
                ["SETRANGE"] = new CommandConfig(CommandFlag.write | CommandFlag.denyoom, CommandTag.write | CommandTag.@string | CommandTag.slow),
                ["STRALGO"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.movablekeys, CommandTag.read | CommandTag.@string | CommandTag.slow),
                ["STRLEN"] = new CommandConfig(CommandFlag.@readonly | CommandFlag.fast, CommandTag.read | CommandTag.@string | CommandTag.fast),
                ["DISCARD"] = new CommandConfig(CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.fast | CommandTag.transaction),
                ["EXEC"] = new CommandConfig(CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale | CommandFlag.skip_monitor | CommandFlag.skip_slowlog, CommandTag.slow | CommandTag.transaction),
                ["MULTI"] = new CommandConfig(CommandFlag.noscript | CommandFlag.loading | CommandFlag.stale | CommandFlag.fast, CommandTag.fast | CommandTag.transaction),
                ["UNWATCH"] = new CommandConfig(CommandFlag.noscript | CommandFlag.fast, CommandTag.fast | CommandTag.transaction),
                ["WATCH"] = new CommandConfig(CommandFlag.noscript | CommandFlag.fast, CommandTag.fast | CommandTag.transaction),
            };
        }
    }

    [Flags]
    public enum CommandFlag : long
    {
        none,
        /// <summary>
        /// command may result in modifications
        /// </summary>
        write,
        /// <summary>
        /// command will never modify keys
        /// </summary>
        @readonly,
        /// <summary>
        /// reject command if currently out of memory
        /// </summary>
        denyoom,
        /// <summary>
        /// server admin command
        /// </summary>
        admin,
        /// <summary>
        /// pubsub-related command
        /// </summary>
        pubsub,
        /// <summary>
        /// deny this command from scripts
        /// </summary>
        noscript,
        /// <summary>
        /// command has random results, dangerous for scripts
        /// </summary>
        random,
        /// <summary>
        /// if called from script, sort output
        /// </summary>
        sort_for_script,
        /// <summary>
        /// allow command while database is loading
        /// </summary>
        loading,
        /// <summary>
        /// allow command while replica has stale data
        /// </summary>
        stale,
        /// <summary>
        /// do not show this command in MONITOR
        /// </summary>
        skip_monitor,
        /// <summary>
        /// cluster related - accept even if importing
        /// </summary>
        asking,
        /// <summary>
        /// command operates in constant or log(N) time. Used for latency monitoring.
        /// </summary>
        fast,
        /// <summary>
        /// keys have no pre-determined position. You must discover keys yourself.
        /// </summary>
        movablekeys,
        no_auth,
        /// <summary>
        /// do not show this command in SLOWLOG
        /// </summary>
        skip_slowlog,
    }

    [Flags]
    public enum CommandTag : long
    {
        none,
        admin,
        bitmap,
        blocking,
        connection,
        dangerous,
        fast,
        geo,
        hash,
        hyperloglog,
        keyspace,
        list,
        pubsub,
        read,
        scripting,
        set,
        slow,
        sortedset,
        stream,
        @string,
        transaction,
        write,
    }
}

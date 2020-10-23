using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using FreeRedis.Internal;
using System.IO;

namespace FreeRedis
{
    public class CommandPacket
    {
        public string _command { get; private set; }
        public string _subcommand { get; private set; }
        public List<object> _input { get; } = new List<object>();
        public List<string> _flagKey { get; } = new List<string>();

        public static implicit operator List<object>(CommandPacket cb) => cb._input;
        public static implicit operator CommandPacket(string cmd) => new CommandPacket().Command(cmd);

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var b = 0; b < _input.Count; b++)
            {
                if (b > 0) sb.Append(" ");
                var tmpstr = _input[b].ToInvariantCultureToString().Replace("\r\n", "\\r\\n");
                if (tmpstr.Length > 96) tmpstr = $"{tmpstr.Substring(0, 96).Trim()}..";
                sb.Append(tmpstr);
            }
            return sb.ToString();
        }

        IRedisSocket _redisSocketPriv;
        internal IRedisSocket _redisSocket { 
            get => _redisSocketPriv;
            set
            {
                _redisSocketPriv = value;
                _readed = false;
                ReadResult = null;
            }
        }
        public bool _writed => _redisSocket != null;
        public bool _readed { get; internal set; }
        public RedisResult ReadResult { get; protected set; }
        public RedisResult<T> Read<T>() => Read<T>(_redisSocket?.Encoding);
        public RedisResult<T> Read<T>(Encoding encoding)
        {
            if (_redisSocket == null) throw new Exception("The command has not been sent");
            if (_readed) return ReadResult as RedisResult<T>;
            _readed = true;
            if (_redisSocket.ClientReply == ClientReplyType.on)
            {
                if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                var rt = RespHelper.Read<T>(_redisSocket.Stream, encoding);
                rt.Encoding = _redisSocket.Encoding;
                ReadResult = rt;
                return rt;
            }
            ReadResult = new RedisResult<T>(default(T), null, true, RedisMessageType.SimpleString) { Encoding = _redisSocket.Encoding };
            return ReadResult as RedisResult<T>;
        }
        public void ReadChunk(Stream destination, int bufferSize = 1024)
        {
            if (_redisSocket == null) throw new Exception("The command has not been sent");
            if (_readed) return;
            _readed = true;
            if (_redisSocket.ClientReply == ClientReplyType.on)
            {
                if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                RespHelper.ReadChunk(_redisSocket.Stream, destination, bufferSize);
            }
        }

        public CommandPacket Command(string cmd, string subcmd = null)
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
        public CommandPacket FlagKey(params string[] keys)
        {
            if (keys != null)
                foreach (var key in keys) 
                    if (!string.IsNullOrEmpty(key)) 
                        _flagKey.Add(key);
            return this;
        }
        public CommandPacket FlagKey(IEnumerable<string> keys)
        {
            if (keys != null)
                foreach (var key in keys)
                    if (!string.IsNullOrEmpty(key))
                        _flagKey.Add(key);
            return this;
        }

        public CommandPacket InputRaw(object arg)
        {
            _input.Add(arg);
            return this;
        }
        public CommandPacket Input(string[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandPacket Input(int[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandPacket Input(long[] args)
        {
            foreach (var arg in args) _input.Add(arg);
            return this;
        }
        public CommandPacket Input(params object[] args) => this.InputIf(true, args);
        public CommandPacket InputIf(bool condition, params object[] args)
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

        public CommandPacket InputKv(KeyValuePair<string, long>[] args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandPacket InputKv(KeyValuePair<string, string>[] args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandPacket InputKv(KeyValuePair<string, object>[] args, Func<object, object> serialize)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, serialize(a.Value) }).SelectMany(a => a).ToArray());
            return this;
        }

        public CommandPacket InputKv(Dictionary<string, long> args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandPacket InputKv(Dictionary<string, string> args)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, a.Value }).SelectMany(a => a).ToArray());
            return this;
        }
        public CommandPacket InputKv(Dictionary<string, object> args, Func<object, object> serialize)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, serialize(a.Value) }).SelectMany(a => a).ToArray());
            return this;
        }
    }

    static class CommandPacketExtensions
    {
        public static CommandPacket SubCommand(this string that, string subcmd) => new CommandPacket().Command(that, subcmd);
        public static CommandPacket Input(this string that, string arg) => new CommandPacket().Command(that).InputRaw(arg);
        public static CommandPacket Input(this string that, long arg) => new CommandPacket().Command(that).InputRaw(arg);
        public static CommandPacket Input(this string that, string arg1, string arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, long arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, int arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, int arg1, int arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, long arg1, long arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, decimal arg2) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, string arg2, string arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, string arg2, long arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, string arg2, decimal arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, long arg2, long arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, decimal arg2, string arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, decimal arg2, decimal arg3) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, long arg2, long arg3, long arg4, decimal arg5) => new CommandPacket().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3).InputRaw(arg4).InputRaw(arg5);
        public static CommandPacket Input(this string that, string[] args) => new CommandPacket().Command(that).Input(args);
    }

    public class CommandSets
    {
        public ServerFlag Flag { get; }
        public ServerTag Tag { get; }
        public LocalStatus Status { get; private set; }

        public CommandSets(ServerFlag flag, ServerTag tag, LocalStatus status)
        {
            this.Flag = flag;
            this.Tag = tag;
            this.Status = status;
        }

        public static List<string> _allCommands { get; }
        static ConcurrentDictionary<string, CommandSets> _dicOptions { get; }
        public static CommandSets Get(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            return _dicOptions.TryGetValue(command?.ToUpper().Trim(), out var options) ? options : null;
        }
        public static void Register(string command, CommandSets options)
        {
            command = command?.ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException(nameof(command));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (_dicOptions.TryAdd(command, options) == false) throw new Exception($"The command \"{command}\" is already registered");
        }

        [Flags]
        public enum LocalStatus : long
        {
            none = 1,
            check_single = 1 << 1,
        }

        static CommandSets()
        {
            #region _allCommands
            _allCommands = new List<string>
            {
                //"BF.RESERVE",
                //"BF.ADD",
                //"BF.MADD",
                //"BF.INSERT",
                //"BF.EXISTS",
                //"BF.MEXISTS",
                //"BF.SCANDUMP",
                //"BF.LOADCHUNK",
                //"BF.INFO",

                //"CMS.INITBYDIM",
                //"CMS.INITBYPROB",
                //"CMS.INCRBY",
                //"CMS.INCRBY",
                //"CMS.QUERY",
                //"CMS.MERGE",
                //"CMS.INFO",

                //"CF.RESERVE",
                //"CF.ADD",
                //"CF.ADDNX",
                //"CF.INSERT",
                //"CF.INSERTNX",
                //"CF.EXISTS",
                //"CF.DEL",
                //"CF.COUNT",
                //"CF.SCANDUMP",
                //"CF.LOADCHUNK",
                //"CF.INFO",

                //"TOPK.RESERVE",
                //"TOPK.ADD",
                //"TOPK.INCRBY",
                //"TOPK.QUERY",
                //"TOPK.COUNT",
                //"TOPK.LIST",
                //"TOPK.INFO",

                //"CLUSTER ADDSLOTS",
                //"CLUSTER BUMPEPOCH",
                //"CLUSTER COUNT-FAILURE-REPORTS",
                //"CLUSTER COUNTKEYSINSLOT",
                //"CLUSTER DELSLOTS",
                //"CLUSTER FAILOVER",
                //"CLUSTER FLUSHSLOTS",
                //"CLUSTER FORGET",
                //"CLUSTER GETKEYSINSLOT",
                //"CLUSTER INFO",
                //"CLUSTER KEYSLOT",
                //"CLUSTER MEET",
                //"CLUSTER MYID",
                //"CLUSTER NODES",
                //"CLUSTER REPLICAS",
                //"CLUSTER REPLICATE",
                //"CLUSTER RESET",
                //"CLUSTER SAVECONFIG",
                //"CLUSTER SET-CONFIG-EPOCH",
                //"CLUSTER SLAVES",
                //"CLUSTER SLOTS",
                //"READONLY",
                //"READWRITE",

                //"AUTH",
                //"CLIENT CACHING",
                //"CLIENT GETNAME",
                //"CLIENT GETREDIR",
                //"CLIENT ID",
                //"CLIENT KILL",
                //"CLIENT LIST",
                //"CLIENT PAUSE",
                //"CLIENT REPLY",
                //"CLIENT SETNAME",
                //"CLIENT TRACKING",
                //"CLIENT UNBLOCK",
                //"ECHO",
                //"HELLO ",
                //"PING ",
                //"QUIT",
                //"SELECT",

                //"GEOADD",
                //"GEODIST",
                //"GEOHASH",
                //"GEOPOS",
                //"GEORADIUS",
                //"GEORADIUSBYMEMBER",

                //"HDEL",
                //"HEXISTS",
                //"HGET",
                //"HGETALL",
                //"HINCRBY",
                //"HINCRBYFLOAT",
                //"HKEYS",
                //"HLEN",
                //"HMGET",
                //"HMSET",
                //"HSCAN",
                //"HSET",
                //"HSETNX",
                //"HSTRLEN",
                //"HVALS",

                //"PFADD",
                //"PFCOUNT",
                //"PFMERGE",

                //"DEL",
                //"DUMP",
                //"EXISTS",
                //"EXPIRE",
                //"EXPIREAT",
                //"KEYS",
                //"MIGRATE",
                //"MOVE",
                //"OBJECT REFCOUNT",
                //"OBJECT IDLETIME",
                //"OBJECT ENCODING",
                //"OBJECT FREQ",
                //"OBJECT HELP",
                //"PERSIST",
                //"PEXPIRE",
                //"PEXPIREAT",
                //"PTTL",
                //"RANDOMKEY",
                //"RENAME",
                //"RENAMENX",
                //"RESTORE",
                //"SCAN",
                //"SORT",
                //"TOUCH",
                //"TTL",
                //"TYPE",
                //"UNLINK",
                //"WAIT",

                //"BLPOP",
                //"BRPOP",
                //"BRPOPLPUSH",
                //"LINDEX",
                //"LINSERT",
                //"LLEN",
                //"LPOP",
                //"LPOS",
                //"LPUSH",
                //"LPUSHX",
                //"LRANGE",
                //"LREM",
                //"LSET",
                //"LTRIM",
                //"RPOP",
                //"RPOPLPUSH",
                //"RPUSH",
                //"RPUSHX",

                //"EVAL",
                //"EVALSHA",
                //"SCRIPT DEBUG",
                //"SCRIPT EXISTS",
                //"SCRIPT FLUSH",
                //"SCRIPT KILL",
                //"SCRIPT LOAD",

                //"ACL CAT",
                //"ACL DELUSER",
                //"ACL GENPASS",
                //"ACL GETUSER",
                //"ACL HELP",
                //"ACL LIST",
                //"ACL LOAD",
                //"ACL LOG",
                //"ACL SAVE",
                //"ACL SETUSER",
                //"ACL USERS",
                //"ACL WHOAMI",
                //"BGREWRITEAOF",
                //"BGSAVE",
                //"COMMAND",
                //"COMMAND COUNT",
                //"COMMAND GETKEYS",
                //"COMMAND INFO",
                //"CONFIG GET",
                //"CONFIG RESETSTAT",
                //"CONFIG REWRITE",
                //"CONFIG SET",
                //"DBSIZE",
                //"DEBUG OBJECT",
                //"DEBUG SEGFAULT",
                //"FLUSHALL",
                //"FLUSHDB",
                //"INFO",
                //"LASTSAVE",
                //"LATENCY DOCTOR",
                //"LATENCY GRAPH",
                //"LATENCY HELP",
                //"LATENCY HISTORY",
                //"LATENCY LATEST",
                //"LATENCY RESET",
                //"LOLWUT",
                //"MEMORY DOCTOR",
                //"MEMORY HELP",
                //"MEMORY MALLOC-STATS",
                //"MEMORY PURGE",
                //"MEMORY STATS",
                //"MEMORY USAGE",
                //"MODULE LIST",
                //"MODULE LOAD",
                //"MODULE UNLOAD",
                //"MONITOR",
                //"REPLICAOF",
                //"ROLE",
                //"SAVE",
                //"SHUTDOWN",
                //"SLOWLOG",
                //"SWAPDB",
                //"SYNC",
                //"TIME",

                //"SADD",
                //"SCARD",
                //"SDIFF",
                //"SDIFFSTORE",
                //"SINTER",
                //"SINTERSTORE",
                //"SISMEMBER",
                //"SMEMBERS",
                //"SMISMEMBER",
                //"SMOVE",
                //"SPOP",
                //"SRANDMEMBER",
                //"SREM",
                //"SSCAN",
                //"SUNION",
                //"SUNIONSTORE",

                //"BZPOPMAX",
                //"BZPOPMIN",
                //"ZADD",
                //"ZCARD",
                //"ZCOUNT",
                //"ZINCRBY",
                //"ZINTERSTORE",
                //"ZLEXCOUNT",
                //"ZPOPMAX",
                //"ZPOPMIN",
                //"ZRANGE",
                //"ZRANGEBYLEX",
                //"ZRANGEBYSCORE",
                //"ZRANK",
                //"ZREM",
                //"ZREMRANGEBYLEX",
                //"ZREMRANGEBYRANK",
                //"ZREMRANGEBYSCORE",
                //"ZREVRANGE",
                //"ZREVRANGEBYLEX",
                //"ZREVRANGEBYSCORE",
                //"ZREVRANK",
                //"ZSCAN",
                //"ZSCORE",

                //"XACK",
                //"XADD",
                //"XCLAIM",
                //"XDEL",
                //"XGROUP CREATE",
                //"XGROUP SETID",
                //"XGROUP DESTROY",
                //"XGROUP DELCONSUMER",
                //"XINFO STREAM",
                //"XINFO GROUPS",
                //"XINFO CONSUMERS",
                //"XLEN",
                //"XPENDING",
                //"XRANGE",
                //"XREVRANGE",
                //"XREAD",
                //"XREADGROUP",
                //"XTRIM",

                //"APPEND",
                //"BITCOUNT",
                //"BITFIELD",
                //"BITOP",
                //"BITPOS",
                //"DECR",
                //"DECRBY",
                //"GET",
                //"GETBIT",
                //"GETRANGE",
                //"GETSET",
                //"INCR",
                //"INCRBY",
                //"INCRBYFLOAT",
                //"MGET",
                //"MSET",
                //"MSETNX",
                //"PSETEX",
                //"SET",
                //"SETBIT",
                //"SETEX",
                //"SETNX",
                //"SETRANGE",
                //"STRALGO",
                //"STRLEN",

                //"DISCARD",
                //"EXEC",
                //"MULTI",
                //"UNWATCH",
                //"WATCH",
            };
            #endregion
            _dicOptions = new ConcurrentDictionary<string, CommandSets>
            {
                #region 此部分是生成的代码
                ["BF.RESERVE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.ADD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.MADD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.INSERT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.EXISTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.MEXISTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.SCANDUMP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.LOADCHUNK"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BF.INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.INITBYDIM"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.INITBYPROB"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.INCRBY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.INCRBY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.QUERY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.MERGE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CMS.INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.RESERVE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.ADD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.ADDNX"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.INSERT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.INSERTNX"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.EXISTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.DEL"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.COUNT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.SCANDUMP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.LOADCHUNK"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CF.INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.RESERVE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.ADD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.INCRBY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.QUERY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.COUNT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.LIST"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["TOPK.INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER ADDSLOTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER BUMPEPOCH"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER COUNT-FAILURE-REPORTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER COUNTKEYSINSLOT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER DELSLOTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER FAILOVER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER FLUSHSLOTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER FORGET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER GETKEYSINSLOT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER KEYSLOT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER MEET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER MYID"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER NODES"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER REPLICAS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER REPLICATE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER RESET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER SAVECONFIG"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER SET-CONFIG-EPOCH"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER SLAVES"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLUSTER SLOTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["READONLY"] = new CommandSets(ServerFlag.fast, ServerTag.keyspace | ServerTag.fast, LocalStatus.none),
                ["READWRITE"] = new CommandSets(ServerFlag.fast, ServerTag.keyspace | ServerTag.fast, LocalStatus.none),
                ["AUTH"] = new CommandSets(ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale | ServerFlag.skip_monitor | ServerFlag.skip_slowlog | ServerFlag.fast | ServerFlag.no_auth, ServerTag.fast | ServerTag.connection, LocalStatus.none),
                ["CLIENT CACHING"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT GETNAME"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT GETREDIR"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT ID"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT KILL"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT LIST"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT PAUSE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT REPLY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT SETNAME"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT TRACKING"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CLIENT UNBLOCK"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ECHO"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.fast | ServerTag.connection, LocalStatus.none),
                ["HELLO "] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["PING "] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["QUIT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SELECT"] = new CommandSets(ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.keyspace | ServerTag.fast, LocalStatus.none),
                ["GEOADD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["GEODIST"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["GEOHASH"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["GEOPOS"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["GEORADIUS"] = new CommandSets(ServerFlag.write | ServerFlag.movablekeys, ServerTag.write | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["GEORADIUSBYMEMBER"] = new CommandSets(ServerFlag.write | ServerFlag.movablekeys, ServerTag.write | ServerTag.geo | ServerTag.slow, LocalStatus.none),
                ["HDEL"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HEXISTS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HGET"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HGETALL"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.hash | ServerTag.slow, LocalStatus.none),
                ["HINCRBY"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HINCRBYFLOAT"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HKEYS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.hash | ServerTag.slow, LocalStatus.none),
                ["HLEN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HMGET"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HMSET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HSCAN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.hash | ServerTag.slow, LocalStatus.none),
                ["HSET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HSETNX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HSTRLEN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.hash | ServerTag.fast, LocalStatus.none),
                ["HVALS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.hash | ServerTag.slow, LocalStatus.none),
                ["PFADD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.hyperloglog | ServerTag.fast, LocalStatus.none),
                ["PFCOUNT"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.hyperloglog | ServerTag.slow, LocalStatus.none),
                ["PFMERGE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.hyperloglog | ServerTag.slow, LocalStatus.none),
                ["DEL"] = new CommandSets(ServerFlag.write, ServerTag.keyspace | ServerTag.write | ServerTag.slow, LocalStatus.none),
                ["DUMP"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.keyspace | ServerTag.read | ServerTag.slow, LocalStatus.none),
                ["EXISTS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["EXPIRE"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["EXPIREAT"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["KEYS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.keyspace | ServerTag.read | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["MIGRATE"] = new CommandSets(ServerFlag.write | ServerFlag.random | ServerFlag.movablekeys, ServerTag.keyspace | ServerTag.write | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["MOVE"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["OBJECT REFCOUNT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["OBJECT IDLETIME"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["OBJECT ENCODING"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["OBJECT FREQ"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["OBJECT HELP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["PERSIST"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["PEXPIRE"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["PEXPIREAT"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["PTTL"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["RANDOMKEY"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.keyspace | ServerTag.read | ServerTag.slow, LocalStatus.none),
                ["RENAME"] = new CommandSets(ServerFlag.write, ServerTag.keyspace | ServerTag.write | ServerTag.slow, LocalStatus.none),
                ["RENAMENX"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["RESTORE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.keyspace | ServerTag.write | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["SCAN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.keyspace | ServerTag.read | ServerTag.slow, LocalStatus.none),
                ["SORT"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.movablekeys, ServerTag.write | ServerTag.set | ServerTag.sortedset | ServerTag.list | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["TOUCH"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["TTL"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["TYPE"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["UNLINK"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast, LocalStatus.none),
                ["WAIT"] = new CommandSets(ServerFlag.noscript, ServerTag.keyspace | ServerTag.slow, LocalStatus.none),
                ["BLPOP"] = new CommandSets(ServerFlag.write | ServerFlag.noscript, ServerTag.write | ServerTag.list | ServerTag.slow | ServerTag.blocking, LocalStatus.none),
                ["BRPOP"] = new CommandSets(ServerFlag.write | ServerFlag.noscript, ServerTag.write | ServerTag.list | ServerTag.slow | ServerTag.blocking, LocalStatus.none),
                ["BRPOPLPUSH"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.noscript, ServerTag.write | ServerTag.list | ServerTag.slow | ServerTag.blocking, LocalStatus.none),
                ["LINDEX"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["LINSERT"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["LLEN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["LPOP"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["LPOS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LPUSH"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["LPUSHX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["LRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["LREM"] = new CommandSets(ServerFlag.write, ServerTag.write | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["LSET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["LTRIM"] = new CommandSets(ServerFlag.write, ServerTag.write | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["RPOP"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["RPOPLPUSH"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.list | ServerTag.slow, LocalStatus.none),
                ["RPUSH"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["RPUSHX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.list | ServerTag.fast, LocalStatus.none),
                ["EVAL"] = new CommandSets(ServerFlag.noscript | ServerFlag.movablekeys, ServerTag.slow | ServerTag.scripting, LocalStatus.none),
                ["EVALSHA"] = new CommandSets(ServerFlag.noscript | ServerFlag.movablekeys, ServerTag.slow | ServerTag.scripting, LocalStatus.none),
                ["SCRIPT DEBUG"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SCRIPT EXISTS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SCRIPT FLUSH"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SCRIPT KILL"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SCRIPT LOAD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL CAT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL DELUSER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL GENPASS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL GETUSER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL HELP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL LIST"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL LOAD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL LOG"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL SAVE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL SETUSER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL USERS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["ACL WHOAMI"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["BGREWRITEAOF"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["BGSAVE"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["COMMAND"] = new CommandSets(ServerFlag.random | ServerFlag.loading | ServerFlag.stale, ServerTag.slow | ServerTag.connection, LocalStatus.none),
                ["COMMAND COUNT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["COMMAND GETKEYS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["COMMAND INFO"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CONFIG GET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CONFIG RESETSTAT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CONFIG REWRITE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["CONFIG SET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["DBSIZE"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.keyspace | ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["DEBUG OBJECT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["DEBUG SEGFAULT"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["FLUSHALL"] = new CommandSets(ServerFlag.write, ServerTag.keyspace | ServerTag.write | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["FLUSHDB"] = new CommandSets(ServerFlag.write, ServerTag.keyspace | ServerTag.write | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["INFO"] = new CommandSets(ServerFlag.random | ServerFlag.loading | ServerFlag.stale, ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["LASTSAVE"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random | ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.read | ServerTag.admin | ServerTag.fast | ServerTag.dangerous, LocalStatus.none),
                ["LATENCY DOCTOR"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LATENCY GRAPH"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LATENCY HELP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LATENCY HISTORY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LATENCY LATEST"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LATENCY RESET"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["LOLWUT"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["MEMORY DOCTOR"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MEMORY HELP"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MEMORY MALLOC-STATS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MEMORY PURGE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MEMORY STATS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MEMORY USAGE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MODULE LIST"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MODULE LOAD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MODULE UNLOAD"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["MONITOR"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["REPLICAOF"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript | ServerFlag.stale, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["ROLE"] = new CommandSets(ServerFlag.@readonly | ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.read | ServerTag.fast | ServerTag.dangerous, LocalStatus.none),
                ["SAVE"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["SHUTDOWN"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["SLOWLOG"] = new CommandSets(ServerFlag.admin | ServerFlag.random | ServerFlag.loading | ServerFlag.stale, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["SWAPDB"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.keyspace | ServerTag.write | ServerTag.fast | ServerTag.dangerous, LocalStatus.none),
                ["SYNC"] = new CommandSets(ServerFlag.admin | ServerFlag.noscript, ServerTag.admin | ServerTag.slow | ServerTag.dangerous, LocalStatus.none),
                ["TIME"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random | ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.read | ServerTag.fast, LocalStatus.none),
                ["SADD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SCARD"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SDIFF"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SDIFFSTORE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SINTER"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SINTERSTORE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SISMEMBER"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SMEMBERS"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SMISMEMBER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["SMOVE"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SPOP"] = new CommandSets(ServerFlag.write | ServerFlag.random | ServerFlag.fast, ServerTag.write | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SRANDMEMBER"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SREM"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.set | ServerTag.fast, LocalStatus.none),
                ["SSCAN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SUNION"] = new CommandSets(ServerFlag.@readonly | ServerFlag.sort_for_script, ServerTag.read | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["SUNIONSTORE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.set | ServerTag.slow, LocalStatus.none),
                ["BZPOPMAX"] = new CommandSets(ServerFlag.write | ServerFlag.noscript | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast | ServerTag.blocking, LocalStatus.none),
                ["BZPOPMIN"] = new CommandSets(ServerFlag.write | ServerFlag.noscript | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast | ServerTag.blocking, LocalStatus.none),
                ["ZADD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZCARD"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZCOUNT"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZINCRBY"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZINTERSTORE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.movablekeys, ServerTag.write | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZLEXCOUNT"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZPOPMAX"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZPOPMIN"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZRANGEBYLEX"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZRANGEBYSCORE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZRANK"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZREM"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZREMRANGEBYLEX"] = new CommandSets(ServerFlag.write, ServerTag.write | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREMRANGEBYRANK"] = new CommandSets(ServerFlag.write, ServerTag.write | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREMRANGEBYSCORE"] = new CommandSets(ServerFlag.write, ServerTag.write | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREVRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREVRANGEBYLEX"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREVRANGEBYSCORE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZREVRANK"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["ZSCAN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.sortedset | ServerTag.slow, LocalStatus.none),
                ["ZSCORE"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.sortedset | ServerTag.fast, LocalStatus.none),
                ["XACK"] = new CommandSets(ServerFlag.write | ServerFlag.random | ServerFlag.fast, ServerTag.write | ServerTag.stream | ServerTag.fast, LocalStatus.none),
                ["XADD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.random | ServerFlag.fast, ServerTag.write | ServerTag.stream | ServerTag.fast, LocalStatus.none),
                ["XCLAIM"] = new CommandSets(ServerFlag.write | ServerFlag.random | ServerFlag.fast, ServerTag.write | ServerTag.stream | ServerTag.fast, LocalStatus.none),
                ["XDEL"] = new CommandSets(ServerFlag.write | ServerFlag.fast, ServerTag.write | ServerTag.stream | ServerTag.fast, LocalStatus.none),
                ["XGROUP CREATE"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XGROUP SETID"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XGROUP DESTROY"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XGROUP DELCONSUMER"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XINFO STREAM"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XINFO GROUPS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XINFO CONSUMERS"] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none),
                ["XLEN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.stream | ServerTag.fast, LocalStatus.none),
                ["XPENDING"] = new CommandSets(ServerFlag.@readonly | ServerFlag.random, ServerTag.read | ServerTag.stream | ServerTag.slow, LocalStatus.none),
                ["XRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.stream | ServerTag.slow, LocalStatus.none),
                ["XREVRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.stream | ServerTag.slow, LocalStatus.none),
                ["XREAD"] = new CommandSets(ServerFlag.@readonly | ServerFlag.movablekeys, ServerTag.read | ServerTag.stream | ServerTag.slow | ServerTag.blocking, LocalStatus.none),
                ["XREADGROUP"] = new CommandSets(ServerFlag.write | ServerFlag.movablekeys, ServerTag.write | ServerTag.stream | ServerTag.slow | ServerTag.blocking, LocalStatus.none),
                ["XTRIM"] = new CommandSets(ServerFlag.write | ServerFlag.random, ServerTag.write | ServerTag.stream | ServerTag.slow, LocalStatus.none),
                ["APPEND"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["BITCOUNT"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.bitmap | ServerTag.slow, LocalStatus.none),
                ["BITFIELD"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.bitmap | ServerTag.slow, LocalStatus.none),
                ["BITOP"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.bitmap | ServerTag.slow, LocalStatus.none),
                ["BITPOS"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.bitmap | ServerTag.slow, LocalStatus.none),
                ["DECR"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["DECRBY"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["GET"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["GETBIT"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.bitmap | ServerTag.fast, LocalStatus.none),
                ["GETRANGE"] = new CommandSets(ServerFlag.@readonly, ServerTag.read | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["GETSET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["INCR"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["INCRBY"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["INCRBYFLOAT"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["MGET"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["MSET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["MSETNX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["PSETEX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["SET"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["SETBIT"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.bitmap | ServerTag.slow, LocalStatus.none),
                ["SETEX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["SETNX"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom | ServerFlag.fast, ServerTag.write | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["SETRANGE"] = new CommandSets(ServerFlag.write | ServerFlag.denyoom, ServerTag.write | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["STRALGO"] = new CommandSets(ServerFlag.@readonly | ServerFlag.movablekeys, ServerTag.read | ServerTag.@string | ServerTag.slow, LocalStatus.none),
                ["STRLEN"] = new CommandSets(ServerFlag.@readonly | ServerFlag.fast, ServerTag.read | ServerTag.@string | ServerTag.fast, LocalStatus.none),
                ["DISCARD"] = new CommandSets(ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.fast | ServerTag.transaction, LocalStatus.none),
                ["EXEC"] = new CommandSets(ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale | ServerFlag.skip_monitor | ServerFlag.skip_slowlog, ServerTag.slow | ServerTag.transaction, LocalStatus.none),
                ["MULTI"] = new CommandSets(ServerFlag.noscript | ServerFlag.loading | ServerFlag.stale | ServerFlag.fast, ServerTag.fast | ServerTag.transaction, LocalStatus.none),
                ["UNWATCH"] = new CommandSets(ServerFlag.noscript | ServerFlag.fast, ServerTag.fast | ServerTag.transaction, LocalStatus.none),
                ["WATCH"] = new CommandSets(ServerFlag.noscript | ServerFlag.fast, ServerTag.fast | ServerTag.transaction, LocalStatus.none),
                #endregion
            };

            Get("AUTH").Status = LocalStatus.check_single;
            Get("CLIENT CACHING").Status = LocalStatus.check_single;
            Get("CLIENT GETNAME").Status = LocalStatus.check_single;
            Get("CLIENT GETREDIR").Status = LocalStatus.check_single;
            Get("CLIENT ID").Status = LocalStatus.check_single;
            Get("CLIENT REPLY").Status = LocalStatus.check_single;
            Get("CLIENT SETNAME").Status = LocalStatus.check_single;
            Get("CLIENT TRACKING").Status = LocalStatus.check_single;
            Get("HELLO").Status = LocalStatus.check_single;
            Get("QUIT").Status = LocalStatus.check_single;
            Get("SELECT").Status = LocalStatus.check_single;
        }

        [Flags]
        public enum ServerFlag : long
        {
            none = 1,
            /// <summary>
            /// command may result in modifications
            /// </summary>
            write = 1 << 1,
            /// <summary>
            /// command will never modify keys
            /// </summary>
            @readonly = 1 << 2,
            /// <summary>
            /// reject command if currently out of memory
            /// </summary>
            denyoom = 1 << 3,
            /// <summary>
            /// server admin command
            /// </summary>
            admin = 1 << 4,
            /// <summary>
            /// pubsub-related command
            /// </summary>
            pubsub = 1 << 5,
            /// <summary>
            /// deny this command from scripts
            /// </summary>
            noscript = 1 << 6,
            /// <summary>
            /// command has random results, dangerous for scripts
            /// </summary>
            random = 1 << 7,
            /// <summary>
            /// if called from script, sort output
            /// </summary>
            sort_for_script = 1 << 8,
            /// <summary>
            /// allow command while database is loading
            /// </summary>
            loading = 1 << 9,
            /// <summary>
            /// allow command while replica has stale data
            /// </summary>
            stale = 1 << 10,
            /// <summary>
            /// do not show this command in MONITOR
            /// </summary>
            skip_monitor = 1 << 11,
            /// <summary>
            /// cluster related - accept even if importing
            /// </summary>
            asking = 1 << 12,
            /// <summary>
            /// command operates in constant or log(N) time. Used for latency monitoring.
            /// </summary>
            fast = 1 << 13,
            /// <summary>
            /// keys have no pre-determined position. You must discover keys yourself.
            /// </summary>
            movablekeys = 1 << 14,
            no_auth = 1 << 15,
            /// <summary>
            /// do not show this command in SLOWLOG
            /// </summary>
            skip_slowlog = 1 << 16,
        }

        [Flags]
        public enum ServerTag : long
        {
            none = 1,
            admin = 1 << 1,
            bitmap = 1 << 2,
            blocking = 1 << 3,
            connection = 1 << 4,
            dangerous = 1 << 5,
            fast = 1 << 6,
            geo = 1 << 7,
            hash = 1 << 8,
            hyperloglog = 1 << 9,
            keyspace = 1 << 10,
            list = 1 << 11,
            pubsub = 1 << 12,
            read = 1 << 13,
            scripting = 1 << 14,
            set = 1 << 15,
            slow = 1 << 16,
            sortedset = 1 << 17,
            stream = 1 << 18,
            @string = 1 << 19,
            transaction = 1 << 20,
            write = 1 << 21,
        }
    }
}

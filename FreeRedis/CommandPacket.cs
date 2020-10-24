using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    public class CommandPacket
    {
        public string _command { get; private set; }
        public string _subcommand { get; private set; }
        public List<object> _input { get; } = new List<object>();
        public List<string> _flagKey { get; } = new List<string>();

        public static implicit operator List<object>(CommandPacket cb) => cb._input;
        public static implicit operator CommandPacket(string cmd) => new CommandPacket(cmd);

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

        public CommandPacket(string cmd, string subcmd = null) => this.Command(cmd, subcmd);
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
        public CommandPacket InputKv(List<KeyValuePair<string, object>> args, Func<object, object> serialize)
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
        public static CommandPacket SubCommand(this string that, string subcmd) => new CommandPacket(that, subcmd);
        public static CommandPacket Input(this string that, string arg) => new CommandPacket(that).InputRaw(arg);
        public static CommandPacket Input(this string that, long arg) => new CommandPacket(that).InputRaw(arg);
        public static CommandPacket Input(this string that, string arg1, string arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, long arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, int arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, int arg1, int arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, long arg1, long arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, decimal arg2) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string that, string arg1, string arg2, string arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, string arg2, long arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, string arg2, decimal arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, long arg2, long arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, decimal arg2, string arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, decimal arg2, decimal arg3) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string that, string arg1, long arg2, long arg3, long arg4, decimal arg5) => new CommandPacket(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3).InputRaw(arg4).InputRaw(arg5);
        public static CommandPacket Input(this string that, string[] args) => new CommandPacket(that).Input(args);
    }
}

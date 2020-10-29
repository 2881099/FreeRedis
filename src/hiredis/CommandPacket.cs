using hiredis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hiredis
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

        internal bool _clusterMovedAsking;
        internal int _clusterMovedTryCount;
        public string WriteHost { get; internal set; }
        public bool _flagReadbytes;
        /// <summary>
        /// read byte[]
        /// </summary>
        /// <param name="isReadbytes"></param>
        /// <returns></returns>
        public CommandPacket FlagReadbytes(bool isReadbytes)
        {
            _flagReadbytes = isReadbytes;
            return this;
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

        public CommandPacket InputKv(object[] keyValues, Func<object, object> serialize)
        {
            if (keyValues == null | keyValues.Length == 0) return this;
            if (keyValues.Length % 2 != 0) throw new ArgumentException($"Array {nameof(keyValues)} length is not even");
            _input.AddRange(keyValues.Select((a, b) => b % 2 == 0 ? a : (serialize?.Invoke(a) ?? a)));
            return this;
        }
        public CommandPacket InputKv<T>(Dictionary<string, T> args, Func<object, object> serialize)
        {
            _input.AddRange(args.Select(a => new object[] { a.Key, serialize?.Invoke(a.Value) ?? a.Value }).SelectMany(a => a).ToArray());
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

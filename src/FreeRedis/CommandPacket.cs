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
        public List<int> _keyIndexes { get; } = new List<int>();
        public string _prefix { get; private set; }

        public string GetKey(int index, bool withoutPrefix = false)
        {
            if (withoutPrefix && !string.IsNullOrWhiteSpace(_prefix))
                return _input[_keyIndexes[index]].ToInvariantCultureToString().Substring(_prefix.Length);
            return _input[_keyIndexes[index]].ToInvariantCultureToString();
        }

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

        internal Queue<Action<RedisResult>> _ondata;
        public CommandPacket OnData(Action<RedisResult> ondata)
        {
            if (_ondata == null) _ondata = new Queue<Action<RedisResult>>();
            _ondata.Enqueue(ondata);
            return this;
        }
        internal void OnDataTrigger(RedisResult rt)
        {
            if (_ondata == null) return;
            while (_ondata.Any())
                _ondata.Dequeue()(rt);
        }

        public CommandPacket Prefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return this;
            if (prefix == _prefix) return this;
            if (!string.IsNullOrWhiteSpace(_prefix))
            {
                foreach (var idx in _keyIndexes)
                {
                    var key = _input[idx].ToInvariantCultureToString();
                    if (key?.StartsWith(_prefix) == true)
                        _input[idx] = key.Substring(_prefix.Length);
                }
            }
            _prefix = prefix;
            foreach (var idx in _keyIndexes)
                _input[idx] = $"{prefix}{_input[idx]}";
            return this;
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

        public CommandPacket InputKey(string key)
        {
            _keyIndexes.Add(_input.Count);
            _input.Add(key);
            return this;
        }
        public CommandPacket InputKey(string[] keys)
        {
            if (keys == null) return this;
            foreach (var key in keys)
            {
                _keyIndexes.Add(_input.Count);
                _input.Add(key);
            }
            return this;
        }
        public CommandPacket InputKeyIf(bool condition, params string[] keys)
        {
            if (condition == false) return this;
            return InputKey(keys);
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

        public CommandPacket InputKv(object[] keyValues, bool iskey, Func<object, object> serialize)
        {
            if (keyValues == null || keyValues.Length == 0) return this;
            if (keyValues.Length % 2 != 0) throw new ArgumentException($"Array {nameof(keyValues)} length is not even");
            for (var a = 0;a < keyValues.Length; a += 2)
            {
                if (iskey) InputKey(keyValues[a].ToInvariantCultureToString());
                else InputRaw(keyValues[a]);
                InputRaw((serialize?.Invoke(keyValues[a + 1]) ?? keyValues[a + 1]));
            }
            return this;
        }
        public CommandPacket InputKv<T>(Dictionary<string, T> keyValues, bool iskey, Func<object, object> serialize)
        {
            foreach (var kv in keyValues)
            {
                if (iskey) InputKey(kv.Key);
                else InputRaw(kv.Key);
                InputRaw(serialize?.Invoke(kv.Value) ?? kv.Value);
            }
            return this;
        }
    }

    static class CommandPacketExtensions
    {
        public static CommandPacket SubCommand(this string cmd, string subcmd) => new CommandPacket(cmd, subcmd);
        public static CommandPacket InputKey(this string cmd, string key) => new CommandPacket(cmd).InputKey(key);

        public static CommandPacket InputKey(this string cmd, string key, string arg1) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1);
        public static CommandPacket InputKey(this string cmd, string key, string arg1, long arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket InputKey(this string cmd, string key, string arg1, decimal arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket InputKey(this string cmd, string key, string arg1, string arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket InputKey(this string cmd, string key, decimal arg1, string arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket InputKey(this string cmd, string key, string[] arg1) => new CommandPacket(cmd).InputKey(key).Input(arg1);

        public static CommandPacket InputKey(this string cmd, string key, long arg1) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1);
        public static CommandPacket InputKey(this string cmd, string key, long arg1, long arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket InputKey(this string cmd, string key, int arg1) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1);

        public static CommandPacket InputKey(this string cmd, string key, decimal arg1) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1);
        public static CommandPacket InputKey(this string cmd, string key, decimal arg1, decimal arg2) => new CommandPacket(cmd).InputKey(key).InputRaw(arg1).InputRaw(arg2);

        public static CommandPacket InputKey(this string cmd, string[] keys) => new CommandPacket(cmd).InputKey(keys);
        public static CommandPacket InputKey(this string cmd, string[] keys, int arg1) => new CommandPacket(cmd).InputKey(keys).InputRaw(arg1);

        public static CommandPacket InputRaw(this string cmd, object arg) => new CommandPacket(cmd).InputRaw(arg);

        public static CommandPacket Input(this string cmd, string arg) => new CommandPacket(cmd).InputRaw(arg);
        public static CommandPacket Input(this string cmd, long arg) => new CommandPacket(cmd).InputRaw(arg);
        public static CommandPacket Input(this string cmd, string arg1, string arg2) => new CommandPacket(cmd).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string cmd, string arg1, int arg2) => new CommandPacket(cmd).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string cmd, int arg1, int arg2) => new CommandPacket(cmd).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string cmd, long arg1, long arg2) => new CommandPacket(cmd).InputRaw(arg1).InputRaw(arg2);
        public static CommandPacket Input(this string cmd, string arg1, string arg2, string arg3) => new CommandPacket(cmd).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandPacket Input(this string cmd, string[] args) => new CommandPacket(cmd).Input(args);
    }
}

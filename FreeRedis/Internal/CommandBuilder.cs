using System;
using System.Collections.Generic;
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
        public CommandBuilder FlagKey(string key)
        {
            if (!string.IsNullOrEmpty(key)) _flagKey.Add(key);
            return this;
        }
        public CommandBuilder FlagKey(IEnumerable<string> keys)
        {
            if (keys != null) _flagKey.AddRange(keys);
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
        public static CommandBuilder Input(this string that, string arg1, string arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, long arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, int arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, int arg1, int arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, string arg2, string arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, string arg2, long arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, long arg2, long arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2, string arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, decimal arg2, decimal arg3) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3);
        public static CommandBuilder Input(this string that, string arg1, long arg2, long arg3, long arg4, decimal arg5) => new CommandBuilder().Command(that).InputRaw(arg1).InputRaw(arg2).InputRaw(arg3).InputRaw(arg4).InputRaw(arg5);
        public static CommandBuilder Input(this string that, string[] args) => new CommandBuilder().Command(that).Input(args);
    }
}

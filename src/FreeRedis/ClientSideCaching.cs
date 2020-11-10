using FreeRedis.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeRedis
{
    public class ClientSideCachingOptions
    {
        public int Capacity { get; set; }

        /// <summary>
        /// true: cache
        /// </summary>
        public Func<string, bool> KeyFilter { get; set; }

        /// <summary>
        /// true: expired
        /// </summary>
        public Func<string, DateTime, bool> CheckExpired { get; set; }
    }

    public static class ClientSideCachingExtensions
    {
        public static void UseClientSideCaching(this RedisClient cli, ClientSideCachingOptions options)
        {
            new ClientSideCachingContext(cli, options)
                .Start();
        }

        class ClientSideCachingContext
        {
            readonly RedisClient _cli;
            readonly ClientSideCachingOptions _options;
            IPubSubSubscriber _sub;

            public ClientSideCachingContext(RedisClient cli, ClientSideCachingOptions options)
            {
                _cli = cli;
                _options = options ?? new ClientSideCachingOptions();
            }

            public void Start()
            {
                _sub = _cli.Subscribe("__redis__:invalidate", InValidate) as IPubSubSubscriber;
                _cli.Interceptors.Add(() => new MemoryCacheAop(this));
                _cli.Unavailable += (_, e) =>
                {
                    lock (_dictLock) _dictSort.Clear();
                    _dict.Clear();
                };
                _cli.Connected += (_, e) =>
                {
                    e.Client.ClientTracking(true, _sub.RedisSocket.ClientId, null, false, false, false, false);
                };
            }

            void InValidate(string chan, object msg)
            {
                var keys = msg as object[];
                foreach (var key in keys)
                    RemoveCache(string.Concat(key));
            }

            static readonly DateTime _dt2020 = new DateTime(2020, 1, 1);
            static long GetTime() => (long)DateTime.Now.Subtract(_dt2020).TotalSeconds;
            /// <summary>
            /// key -> Type(string|byte[]|class) -> value
            /// </summary>
            readonly ConcurrentDictionary<string, DictValue> _dict = new ConcurrentDictionary<string, DictValue>();
            readonly SortedSet<string> _dictSort = new SortedSet<string>();
            readonly object _dictLock = new object();
            bool TryGetCacheValue(string key, Type valueType, out object value)
            {
                if (_dict.TryGetValue(key, out var trydictval) && trydictval.Values.TryGetValue(valueType, out var tryval)
                    //&& DateTime.Now.Subtract(_dt2020.AddSeconds(tryval.SetTime)) < TimeSpan.FromMinutes(5)
                    )
                {
                    if (_options.CheckExpired?.Invoke(key, _dt2020.AddSeconds(tryval.SetTime)) == true)
                    {
                        RemoveCache(key);
                        value = null;
                        return false;
                    }
                    var time = GetTime();
                    if (_options.Capacity > 0)
                    {
                        lock (_dictLock)
                        {
                            _dictSort.Remove($"{trydictval.GetTime.ToString("X").PadLeft(16, '0')}{key}");
                            _dictSort.Add($"{time.ToString("X").PadLeft(16, '0')}{key}");
                        }
                    }
                    Interlocked.Exchange(ref trydictval.GetTime, time);
                    value = tryval.Value;
                    return true;
                }
                value = null;
                return false;
            }
            void SetCacheValue(string command, string key, Type valueType, object value)
            {
                _dict.GetOrAdd(key, keyTmp =>
                {
                    var time = GetTime();
                    if (_options.Capacity > 0)
                    {
                        string removeKey = null;
                        lock (_dictLock)
                        {
                            if (_dictSort.Count >= _options.Capacity) removeKey = _dictSort.First().Substring(16);
                            _dictSort.Add($"{time.ToString("X").PadLeft(16, '0')}{key}");
                        }
                        if (removeKey != null)
                            RemoveCache(removeKey);
                    }
                    return new DictValue(command, time);
                }).Values
                .AddOrUpdate(valueType, new DictValue.ObjectValue(value), (oldkey, oldval) => new DictValue.ObjectValue(value));
            }
            void RemoveCache(params string[] keys)
            {
                if (keys?.Any() != true) return;
                foreach (var key in keys)
                {
                    if (_dict.TryRemove(key, out var old))
                    {
                        if (_options.Capacity > 0)
                        {
                            lock (_dictLock)
                            {
                                _dictSort.Remove($"{old.GetTime.ToString("X").PadLeft(16, '0')}{key}");
                            }
                        }
                    }
                }
            }
            class DictValue
            {
                public readonly ConcurrentDictionary<Type, ObjectValue> Values = new ConcurrentDictionary<Type, ObjectValue>();
                public readonly string Command;
                public long GetTime;
                public DictValue(string command, long gettime)
                {
                    this.Command = command;
                    this.GetTime = gettime;
                }
                public class ObjectValue
                {
                    public readonly object Value;
                    public readonly long SetTime = (long)DateTime.Now.Subtract(_dt2020).TotalSeconds;
                    public ObjectValue(object value) => this.Value = value;
                }
            }

            class MemoryCacheAop : IInterceptor
            {
                ClientSideCachingContext _cscc;
                public MemoryCacheAop(ClientSideCachingContext cscc)
                {
                    _cscc = cscc;
                }

                bool _iscached = false;
                public void Before(InterceptorBeforeEventArgs args)
                {
                    switch (args.Command._command)
                    {
                        case "GET":
                            if (_cscc.TryGetCacheValue(args.Command.GetKey(0), args.ValueType, out var getval))
                            {
                                args.Value = getval;
                                _iscached = true;
                            }
                            break;
                        case "MGET":
                            var mgetValType = args.ValueType.GetElementType();
                            var mgetKeys = args.Command._keyIndexes.Select((item, index) => args.Command.GetKey(index)).ToArray();
                            var mgetVals = mgetKeys.Select(a => _cscc.TryGetCacheValue(a, mgetValType, out var mgetval) ?
                                    new DictGetResult { Value = mgetval, Exists = true } : new DictGetResult { Value = null, Exists = false })
                                .Where(a => a.Exists).Select(a => a.Value).ToArray();
                            if (mgetVals.Length == mgetKeys.Length)
                            {
                                args.Value = args.ValueType.FromObject(mgetVals);
                                _iscached = true;
                            }
                            break;
                    }
                }

                public void After(InterceptorAfterEventArgs args)
                {
                    switch (args.Command._command)
                    {
                        case "GET":
                            if (_iscached == false && args.Exception == null)
                            {
                                var getkey = args.Command.GetKey(0);
                                if (_cscc._options.KeyFilter?.Invoke(getkey) != false)
                                    _cscc.SetCacheValue(args.Command._command, getkey, args.ValueType, args.Value);
                            }
                            break;
                        case "MGET":
                            if (_iscached == false && args.Exception == null)
                            {
                                if (args.Value is Array valueArr)
                                {
                                    var valueArrElementType = args.ValueType.GetElementType();
                                    var sourceArrLen = valueArr.Length;
                                    for (var a = 0; a < sourceArrLen; a++)
                                        _cscc.SetCacheValue("GET", args.Command.GetKey(a), valueArrElementType, valueArr.GetValue(a));
                                }
                            }
                            break;
                        default:
                            if (args.Command._keyIndexes.Any())
                            {
                                var cmdset = CommandSets.Get(args.Command._command);
                                if (cmdset != null &&
                                    (cmdset.Flag & CommandSets.ServerFlag.write) == CommandSets.ServerFlag.write &&
                                    (cmdset.Tag & CommandSets.ServerTag.write) == CommandSets.ServerTag.write &&
                                    (cmdset.Tag & CommandSets.ServerTag.@string) == CommandSets.ServerTag.@string)
                                {
                                    _cscc.RemoveCache(args.Command._keyIndexes.Select((item, index) => args.Command.GetKey(index)).ToArray());
                                }
                            }
                            break;
                    }
                }

                class DictGetResult
                {
                    public object Value;
                    public bool Exists;
                }
            }
        }
    }
}

using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeRedis
{
    partial class RedisClient
    {
        class SentinelAdapter : BaseAdapter
        {
            readonly RedisClient _cli;
            readonly IdleBus<RedisClientPool> _ib;
            readonly ConnectionStringBuilder _connectionString;
            readonly LinkedList<string> _sentinels;
            string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;

            public SentinelAdapter(RedisClient cli, ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
            {
                UseType = UseType.Sentinel;
                _cli = cli;
                _connectionString = sentinelConnectionString;
                _sentinels = new LinkedList<string>(sentinels?.Select(a => a.ToLower()).Distinct() ?? new string[0]);
                _rw_splitting = rw_splitting;
                _is_single = !_rw_splitting && sentinelConnectionString.MaxPoolSize == 1;
                if (_sentinels.Any() == false) throw new ArgumentNullException(nameof(sentinels));

                _ib = new IdleBus<RedisClientPool>();
                _ib.Notice += new EventHandler<IdleBus<string, RedisClientPool>.NoticeEventArgs>((_, e) => { });
                ResetSentinel();
            }

            public override void Dispose()
            {
                _ib.Dispose();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                if (cmd != null && (_rw_splitting || !_is_single))
                {
                    var cmdset = CommandSets.Get(cmd._command);
                    if (cmdset != null)
                    {
                        if (!_is_single && (cmdset.Status & CommandSets.LocalStatus.check_single) == CommandSets.LocalStatus.check_single)
                            throw new RedisException($"RedisClient: Method cannot be used in {UseType} mode. You can set \"max pool size=1\", but it is not singleton mode.");

                        if (_rw_splitting &&
                            ((cmdset.Tag & CommandSets.ServerTag.read) == CommandSets.ServerTag.read ||
                            (cmdset.Flag & CommandSets.ServerFlag.@readonly) == CommandSets.ServerFlag.@readonly))
                        {
                            var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _masterHost);
                            if (rndkeys.Any())
                            {
                                var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                var rndpool = _ib.Get(rndkey);
                                var rndcli = rndpool.Get();
                                var rndrds = rndcli.Value.Adapter.GetRedisSocket(null);
                                return DefaultRedisSocket.CreateTempProxy(rndrds, () => rndpool.Return(rndcli));
                            }
                        }
                    }
                }
                var poolkey = _masterHost;
                if (string.IsNullOrWhiteSpace(poolkey)) throw new Exception("RedisClient.GetRedisSocket() Redis Sentinel Master is switching");
                var pool = _ib.Get(poolkey);
                var cli = pool.Get();
                var rds = cli.Value.Adapter.GetRedisSocket(null);
                return DefaultRedisSocket.CreateTempProxy(rds, () => pool.Return(cli));
            }
            public override T2 AdapaterCall<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                return _cli.LogCall(cmd, () =>
                {
                    using (var rds = GetRedisSocket(cmd))
                    {
                        rds.Write(cmd);
                        var rt = cmd.Read<T1>();
                        rt.IsErrorThrow = _cli._isThrowRedisSimpleError;
                        return parse(rt);
                    }
                });
            }

            int _ResetSentinelFlag = 0;
            internal void ResetSentinel()
            {
                if (_ResetSentinelFlag != 0) return;
                if (Interlocked.Increment(ref _ResetSentinelFlag) != 1)
                {
                    Interlocked.Decrement(ref _ResetSentinelFlag);
                    return;
                }
                string masterhostEnd = _masterHost;
                var allkeys = _ib.GetKeys().ToList();

                for (int i = 0; i < _sentinels.Count; i++)
                {
                    if (i > 0)
                    {
                        var first = _sentinels.First;
                        _sentinels.RemoveFirst();
                        _sentinels.AddLast(first.Value);
                    }

                    try
                    {
                        using (var sentinelcli = new RedisSentinelClient(_sentinels.First.Value))
                        {
                            var masterhost = sentinelcli.GetMasterAddrByName(_connectionString.Host);
                            var masterConnectionString = localTestHost(masterhost, Model.RoleType.Master);
                            if (masterConnectionString == null) continue;
                            masterhostEnd = masterhost;

                            if (_rw_splitting)
                            {
                                foreach (var slave in sentinelcli.Salves(_connectionString.Host))
                                {
                                    ConnectionStringBuilder slaveConnectionString = localTestHost($"{slave.ip}:{slave.port}", Model.RoleType.Slave);
                                    if (slaveConnectionString == null) continue;
                                }
                            }

                            foreach (var sentinel in sentinelcli.Sentinels(_connectionString.Host))
                            {
                                var remoteSentinelHost = $"{sentinel.ip}:{sentinel.port}";
                                if (_sentinels.Contains(remoteSentinelHost)) continue;
                                _sentinels.AddLast(remoteSentinelHost);
                            }
                        }
                        break;
                    }
                    catch { }
                }

                foreach (var spkey in allkeys) _ib.TryRemove(spkey);
                Interlocked.Exchange(ref _masterHost, masterhostEnd);
                Interlocked.Decrement(ref _ResetSentinelFlag);

                ConnectionStringBuilder localTestHost(string host, Model.RoleType role)
                {
                    ConnectionStringBuilder connectionString = _connectionString.ToString();
                    connectionString.Host = host;
                    connectionString.MinPoolSize = 1;
                    connectionString.MaxPoolSize = 1;
                    using (var cli = new RedisClient(connectionString))
                    {
                        if (cli.Role().role != role)
                            return null;

                        if (role == Model.RoleType.Master)
                        {
                            //test set/get
                        }
                    }
                    connectionString.MinPoolSize = connectionString.MinPoolSize;
                    connectionString.MaxPoolSize = connectionString.MaxPoolSize;

                    _ib.TryRegister(host, () => new RedisClientPool(connectionString, null, _cli.Serialize, _cli.Deserialize));
                    allkeys.Remove(host);

                    return connectionString;
                }
            }
            bool SentinelBackgroundGetMasterHostIng = false;
            object SentinelBackgroundGetMasterHostIngLock = new object();
            bool SentinelBackgroundGetMasterHost(IRedisSocket rds)
            {
                if (rds == null) return false;
                //if (rds._host != _masterHost) return false;

                var ing = false;
                if (SentinelBackgroundGetMasterHostIng == false)
                    lock (SentinelBackgroundGetMasterHostIngLock)
                    {
                        if (SentinelBackgroundGetMasterHostIng == false)
                            SentinelBackgroundGetMasterHostIng = ing = true;
                    }

                if (ing)
                {
                    new Thread(() =>
                    {
                        while (true)
                        {
                            Thread.CurrentThread.Join(1000);
                            try
                            {
                                ResetSentinel();

                                if (_ib.Get(_masterHost).CheckAvailable())
                                {
                                    var bgcolor = Console.BackgroundColor;
                                    var forecolor = Console.ForegroundColor;
                                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.Write($"Redis Sentinel Pool 已切换至 {_masterHost}");
                                    Console.BackgroundColor = bgcolor;
                                    Console.ForegroundColor = forecolor;
                                    Console.WriteLine();

                                    SentinelBackgroundGetMasterHostIng = false;
                                    return;
                                }
                            }
                            catch (Exception ex21)
                            {
                                Console.WriteLine($"Redis Sentinel: {ex21.Message}");
                            }
                        }
                    }).Start();
                }
                return ing;
            }
        }
    }
}
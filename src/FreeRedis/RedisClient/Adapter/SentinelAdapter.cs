using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        class SentinelAdapter : BaseAdapter
        {
            readonly IdleBus<RedisClientPool> _ib;
            readonly ConnectionStringBuilder _connectionString;
            readonly LinkedList<ConnectionStringBuilder> _sentinels;
            string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;

            public SentinelAdapter(RedisClient topOwner, ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
            {
                UseType = UseType.Sentinel;
                TopOwner = topOwner;
                _connectionString = sentinelConnectionString;
                _sentinels = new LinkedList<ConnectionStringBuilder>(sentinels?.Select(a =>
                {
                    var csb = ConnectionStringBuilder.Parse(a);
                    csb.Host = csb.Host.ToLower();
                    return csb;
                }).GroupBy(a => a.Host, a => a).Select(a => a.First()) ?? new ConnectionStringBuilder[0]);
                _rw_splitting = rw_splitting;

                _is_single = !_rw_splitting && sentinelConnectionString.MaxPoolSize == 1;
                if (_sentinels.Any() == false) throw new ArgumentNullException(nameof(sentinels));

                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                ResetSentinel();

#if isasync
                _asyncManager = new AsyncRedisSocket.Manager(this);
#endif
            }

            public override void Dispose()
            {
                _ib.Dispose();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
                var tmprds = redisSocket as DefaultRedisSocket.TempProxyRedisSocket;
                if (tmprds != null) _ib.Get(tmprds._poolkey);
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                var poolkey = GetIdleBusKey(cmd);
                if (string.IsNullOrWhiteSpace(poolkey)) throw new RedisClientException($"【{_connectionString.Host}】Redis Sentinel is switching");
                var pool = _ib.Get(poolkey);
                var cli = pool.Get();
                var rds = cli.Value.Adapter.GetRedisSocket(null);
                var rdsproxy = DefaultRedisSocket.CreateTempProxy(rds, () => pool.Return(cli));
                rdsproxy._poolkey = poolkey;
                rdsproxy._pool = pool;
                return rdsproxy;
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    RedisResult rt = null;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        try
                        {
                            rds.Write(cmd);
                            rt = rds.Read(cmd);
                        }
                        catch (Exception ex)
                        {
                            var pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            if (pool?.SetUnavailable(ex) == true)
                            {
                                Interlocked.Exchange(ref _masterHost, null);
                                RecoverySentinel();
                            }
                            throw ex;
                        }
                    }
                    return parse(rt);
                });
            }
#if isasync
            AsyncRedisSocket.Manager _asyncManager;
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCallAsync(cmd, async () =>
                {
                    var asyncRds = _asyncManager.GetAsyncRedisSocket(cmd);
                    var rt = await asyncRds.WriteAsync(cmd);
                    return parse(rt);
                });
            }
#endif

            string GetIdleBusKey(CommandPacket cmd)
            {
                if (cmd != null && (_rw_splitting || !_is_single))
                {
                    var cmdset = CommandSets.Get(cmd._command);
                    if (cmdset != null)
                    {
                        if (!_is_single && (cmdset.Status & CommandSets.LocalStatus.check_single) == CommandSets.LocalStatus.check_single)
                            throw new RedisClientException($"Method cannot be used in {UseType} mode. You can set \"max pool size=1\", but it is not singleton mode.");

                        if (_rw_splitting &&
                            ((cmdset.Tag & CommandSets.ServerTag.read) == CommandSets.ServerTag.read ||
                            (cmdset.Flag & CommandSets.ServerFlag.@readonly) == CommandSets.ServerFlag.@readonly))
                        {
                            var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _masterHost);
                            if (rndkeys.Any())
                            {
                                var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                return rndkey;
                            }
                        }
                    }
                }
                return _masterHost;
            }

            int ResetSentinelFlag = 0;
            internal void ResetSentinel()
            {
                if (ResetSentinelFlag != 0) return;
                if (Interlocked.Increment(ref ResetSentinelFlag) != 1)
                {
                    Interlocked.Decrement(ref ResetSentinelFlag);
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
                            var masterConnectionString = localTestHost(masterhost, RoleType.Master);
                            if (masterConnectionString == null) continue;
                            masterhostEnd = masterhost;

                            if (_rw_splitting)
                            {
                                foreach (var slave in sentinelcli.Salves(_connectionString.Host))
                                {
                                    ConnectionStringBuilder slaveConnectionString = localTestHost($"{slave.ip}:{slave.port}", RoleType.Slave);
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

                foreach (var spkey in allkeys) _ib.TryRemove(spkey, true);
                Interlocked.Exchange(ref _masterHost, masterhostEnd);
                Interlocked.Decrement(ref ResetSentinelFlag);

                ConnectionStringBuilder localTestHost(string host, RoleType role)
                {
                    ConnectionStringBuilder connectionString = _connectionString.ToString();
                    connectionString.Host = host;
                    connectionString.MinPoolSize = 1;
                    connectionString.MaxPoolSize = 1;
                    using (var cli = new RedisClient(connectionString))
                    {
                        if (cli.Role().role != role)
                            return null;

                        if (role == RoleType.Master)
                        {
                            //test set/get
                        }
                    }
                    connectionString.MinPoolSize = connectionString.MinPoolSize;
                    connectionString.MaxPoolSize = connectionString.MaxPoolSize;

                    _ib.TryRegister(host, () => new RedisClientPool(connectionString, null, TopOwner));
                    allkeys.Remove(host);

                    return connectionString;
                }
            }

            bool RecoverySentineling = false;
            object RecoverySentinelingLock = new object();
            bool RecoverySentinel()
            {
                var ing = false;
                if (RecoverySentineling == false)
                    lock (RecoverySentinelingLock)
                    {
                        if (RecoverySentineling == false)
                            RecoverySentineling = ing = true;
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
                                    if (!TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Info, null, $"{_connectionString.Host.PadRight(21)} > Redis Sentinel switch to {_masterHost}", null)))
                                    {
                                        var bgcolor = Console.BackgroundColor;
                                        var forecolor = Console.ForegroundColor;
                                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.Write($"【{_connectionString.Host}】Redis Sentinel switch to {_masterHost}");
                                        Console.BackgroundColor = bgcolor;
                                        Console.ForegroundColor = forecolor;
                                        Console.WriteLine();
                                    }

                                    RecoverySentineling = false;
                                    return;
                                }
                            }
                            catch (Exception ex21)
                            {
                                if (!TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Info, null, $"{_connectionString.Host.PadRight(21)} > Redis Sentinel switch to {_masterHost}", null)))
                                {
                                    Console.WriteLine($"【{_connectionString.Host}】Redis Sentinel: {ex21.Message}");
                                }
                            }
                        }
                    }).Start();
                }
                return ing;
            }
        }
    }
}
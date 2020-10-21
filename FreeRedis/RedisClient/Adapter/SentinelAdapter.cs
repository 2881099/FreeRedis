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
            readonly IdleBus<RedisClientPool> _ib;
            readonly ConnectionStringBuilder _connectionString;
            readonly LinkedList<string> _sentinels;
            string _masterHost;
            readonly bool _rw_splitting;

            public SentinelAdapter(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
            {
                UseType = UseType.Sentinel;
                _connectionString = sentinelConnectionString;
                _sentinels = new LinkedList<string>(sentinels?.Select(a => a.ToLower()).Distinct() ?? new string[0]);
                _rw_splitting = rw_splitting;
                if (_sentinels.Any() == false) throw new ArgumentNullException(nameof(sentinels));

                _ib = new IdleBus<RedisClientPool>();
                ResetSentinel();
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                if (_ib.Get(_masterHost).Policy.PoolSize != 1)
                    throw new RedisException($"RedisClient: Method cannot be used in {UseType} mode. You can set \"max pool size=1\", but it is not singleton mode.");
                return func();
            }

            public override void Dispose()
            {
                _ib.Dispose();
            }
            public override void Reset()
            {
                throw new NotImplementedException();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                if (_rw_splitting && cmd != null)
                {
                    var cmdcfg = CommandConfig.Get(cmd._command);
                    if (cmdcfg != null)
                    {
                        if (
                            (cmdcfg.Tag | CommandTag.read) == CommandTag.read &&
                            (cmdcfg.Flag | CommandFlag.@readonly) == CommandFlag.@readonly)
                        {
                            var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _masterHost);
                            if (rndkeys.Any())
                            {
                                var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                var rndpool = _ib.Get(rndkey);
                                var rndcli = rndpool.Get();
                                var rndrds = rndcli.Value._adapter.GetRedisSocket(null);
                                return new DefaultRedisSocket.TempRedisSocket(rndrds, rndkey, () => rndpool.Return(rndcli));
                            }
                        }
                    }
                }
                var poolkey = _masterHost;
                if (string.IsNullOrWhiteSpace(poolkey)) throw new Exception("RedisClient.GetRedisSocket() Redis Sentinel Master is switching");
                var pool = _ib.Get(poolkey);
                var cli = pool.Get();
                var rds = cli.Value._adapter.GetRedisSocket(null);
                return new DefaultRedisSocket.TempRedisSocket(rds, poolkey, () => pool.Return(cli));
            }
            public override T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                using (var rds = GetRedisSocket(cmd))
                {
                    rds.Write(cmd);
                    var rt = cmd.Read<T1>();
                    return parse(rt);
                }
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

                    _ib.TryRegister(host, () => new RedisClientPool(connectionString, null));
                    allkeys.Remove(host);

                    return connectionString;
                }
            }
            bool SentinelBackgroundGetMasterHostIng = false;
            object SentinelBackgroundGetMasterHostIngLock = new object();
            bool SentinelBackgroundGetMasterHost(DefaultRedisSocket.TempRedisSocket rds)
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
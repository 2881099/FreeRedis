﻿using FreeRedis.Internal;
using FreeRedis.Internal.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        internal class SentinelAdapter : BaseAdapter
        {
            internal readonly IdleBus<RedisClientPool> _ib;
            internal readonly ConnectionStringBuilder _connectionString;
            readonly LinkedList<ConnectionStringBuilder> _sentinels;
            string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;
            Exception _switchingException;

            public SentinelAdapter(RedisClient topOwner, ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
            {
                UseType = UseType.Sentinel;
                TopOwner = topOwner;
                _connectionString = sentinelConnectionString;
                _sentinels = new LinkedList<ConnectionStringBuilder>(sentinels?.Select(a =>
                {
                    var csb = ConnectionStringBuilder.Parse(a);
                    csb.Host = csb.Host.ToLower();
                    csb.CertificateValidation = _connectionString.CertificateValidation;
                    csb.CertificateSelection = _connectionString.CertificateSelection;
                    return csb;
                }).GroupBy(a => a.Host, a => a).Select(a => a.First()) ?? new ConnectionStringBuilder[0]);
                _rw_splitting = rw_splitting;

                _is_single = !_rw_splitting && sentinelConnectionString.MaxPoolSize == 1;
                if (_sentinels.Any() == false) throw new ArgumentNullException(nameof(sentinels));

                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                ResetSentinel();
            }

            bool isdisposed = false;
            public override void Dispose()
            {
                foreach (var key in _ib.GetKeys())
                {
                    var pool = _ib.Get(key);
                    TopOwner.Unavailable?.Invoke(TopOwner, new UnavailableEventArgs(pool.Key, pool));
                }
                isdisposed = true;
                _ib.Dispose();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
                if (isdisposed) return;
                var tmprds = redisSocket as DefaultRedisSocket.TempProxyRedisSocket;
                if (tmprds != null) _ib.Get(tmprds._poolkey);
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                var poolkey = GetIdleBusKey(cmd);
                if (string.IsNullOrWhiteSpace(poolkey)) throw new RedisClientException($"【{_connectionString.Host}】Redis Sentinel is switching{(_switchingException == null ? "" : $", {_switchingException.Message}")}");
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
                    var protocolRetry = false;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        var getTime = DateTime.Now;
                        try
                        {
                            rds.Write(cmd);
                            rt = rds.Read(cmd);
                        }
                        catch (ProtocolViolationException)
                        {
                            var pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            rds.ReleaseSocket();
                            cmd._protocolErrorTryCount++;
                            if (cmd._protocolErrorTryCount <= pool._policy._connectionStringBuilder.Retry)
                                protocolRetry = true;
                            else
                            {
                                if (cmd.IsReadOnlyCommand() == false || cmd._protocolErrorTryCount > 1) throw;
                                protocolRetry = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            var pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            if (pool?.SetUnavailable(ex, getTime) == true)
                            {
                                RecoverySentinel();
                            }
                            throw;
                        }
                    }
                    if (protocolRetry) return AdapterCall(cmd, parse);
                    return parse(rt);
                });
            }
#if isasync
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCallAsync(cmd, async () =>
                {
                    RedisResult rt = null;
                    var protocolRetry = false;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        var getTime = DateTime.Now;
                        try
                        {
                            await rds.WriteAsync(cmd);
                            rt = await rds.ReadAsync(cmd);
                        }
                        catch (ProtocolViolationException)
                        {
                            var pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            rds.ReleaseSocket();
                            cmd._protocolErrorTryCount++;
                            if (cmd._protocolErrorTryCount <= pool._policy._connectionStringBuilder.Retry)
                                protocolRetry = true;
                            else
                            {
                                if (cmd.IsReadOnlyCommand() == false || cmd._protocolErrorTryCount > 1) throw;
                                protocolRetry = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            var pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            if (pool?.SetUnavailable(ex, getTime) == true)
                            {
                                RecoverySentinel();
                            }
                            throw;
                        }
                    }
                    if (protocolRetry) return await AdapterCallAsync(cmd, parse);
                    return parse(rt);
                });
            }
#endif

            internal string GetIdleBusKey(CommandPacket cmd)
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
                _switchingException = null;
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
                                ConnectionStringBuilder remoteSentinelHost = _sentinels.First.Value.ToString();
                                remoteSentinelHost.Host = $"{sentinel.ip}:{sentinel.port}";
                                remoteSentinelHost.CertificateValidation = _connectionString.CertificateValidation;
                                remoteSentinelHost.CertificateSelection = _connectionString.CertificateSelection;
                                if (_sentinels.Any(a => string.Compare(a.Host, remoteSentinelHost.Host, true) == 0)) continue;
                                _sentinels.AddLast(remoteSentinelHost);
                            }
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        _switchingException = ex;
                    }
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
                    connectionString.CertificateValidation = _connectionString.CertificateValidation;
                    connectionString.CertificateSelection = _connectionString.CertificateSelection;
                    using (var cli = new RedisClient(connectionString))
                    {
                        if (cli.Role().role != role)
                            return null;

                        if (role == RoleType.Master)
                        {
                            //test set/get
                        }
                    }
                    connectionString.MinPoolSize = _connectionString.MinPoolSize;
                    connectionString.MaxPoolSize = _connectionString.MaxPoolSize;

                    _ib.TryRegister(host, () => new RedisClientPool(connectionString, TopOwner));
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
                                        TestTrace.WriteLine($"【{_connectionString.Host}】Redis Sentinel switch to {_masterHost}", ConsoleColor.DarkGreen);

                                    RecoverySentineling = false;
                                    return;
                                }
                            }
                            catch (Exception ex21)
                            {
                                if (!TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Info, null, $"{_connectionString.Host.PadRight(21)} > Redis Sentinel switch to {_masterHost}", null)))
                                    TestTrace.WriteLine($"【{_connectionString.Host}】Redis Sentinel: {ex21.Message}", ConsoleColor.DarkYellow);
                            }
                        }
                    }).Start();
                }
                return ing;
            }
        }
    }
}
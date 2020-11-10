using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FreeRedis
{
    partial class RedisClient
    {
        class PoolingAdapter : BaseAdapter
        {
            readonly IdleBus<RedisClientPool> _ib;
            readonly string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;

            public PoolingAdapter(RedisClient topOwner, ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
            {
                UseType = UseType.Pooling;
                TopOwner = topOwner;
                _masterHost = connectionString.Host;
                _rw_splitting = slaveConnectionStrings?.Any() == true;
                _is_single = !_rw_splitting && connectionString.MaxPoolSize == 1;

                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                _ib.Register(_masterHost, () => new RedisClientPool(connectionString, null, TopOwner));

                if (_rw_splitting)
                    foreach (var slave in slaveConnectionStrings)
                        _ib.TryRegister($"slave_{slave.Host}", () => new RedisClientPool(slave, null, TopOwner));

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
                    RedisClientPool pool = null;
                    Exception ioex = null;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                        try
                        {
                            rds.Write(cmd);
                            rt = rds.Read(cmd);
                        }
                        catch (Exception ex)
                        {
                            ioex = ex;
                        }
                    }
                    if (ioex != null)
                    {
                        if (pool?.SetUnavailable(ioex) == true)
                        {
                        }
                        throw ioex;
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
        }
    }
}
using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        class PoolingAdapter : BaseAdapter
        {
            readonly RedisClient _cli;
            readonly IdleBus<RedisClientPool> _ib;
            readonly string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;

            public PoolingAdapter(RedisClient cli, ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
            {
                UseType = UseType.Pooling; 
                _cli = cli;
                _masterHost = connectionString.Host;
                _rw_splitting = slaveConnectionStrings?.Any() == true;
                _is_single = !_rw_splitting && connectionString.MaxPoolSize == 1;

                _ib = new IdleBus<RedisClientPool>();
                _ib.Notice += new EventHandler<IdleBus<string, RedisClientPool>.NoticeEventArgs>((_, e) => { });
                _ib.Register(_masterHost, () => new RedisClientPool(connectionString, null, _cli.Serialize, _cli.Deserialize));

                if (_rw_splitting)
                    foreach (var slave in slaveConnectionStrings)
                        _ib.TryRegister($"slave_{slave.Host}", () => new RedisClientPool(slave, null, _cli.Serialize, _cli.Deserialize));
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
        }
    }
}
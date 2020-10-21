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
            readonly IdleBus<RedisClientPool> _ib;
            readonly string _masterHost;
            readonly bool _rw_plitting;

            public PoolingAdapter(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
            {
                UseType = UseType.Pooling;
                _masterHost = connectionString.Host;
                _rw_plitting = slaveConnectionStrings?.Any() == true;

                _ib = new IdleBus<RedisClientPool>();
                _ib.Register(_masterHost, () => new RedisClientPool(connectionString, null));

                if (_rw_plitting)
                    foreach (var slave in slaveConnectionStrings)
                        _ib.TryRegister($"slave_{slave.Host}", () => new RedisClientPool(slave, null));
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
                if (_rw_plitting && cmd != null)
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
        }
    }
}
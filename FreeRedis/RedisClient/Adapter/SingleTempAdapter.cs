using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        class SingleTempAdapter : BaseAdapter
        {
            readonly RedisClient _cli;
            readonly IRedisSocket _redisSocket;
            readonly Action _dispose;

            public SingleTempAdapter(RedisClient cli, IRedisSocket redisSocket, Action dispose)
            {
                UseType = UseType.SingleInside;
                _cli = cli;
                _redisSocket = redisSocket;
                _dispose = dispose;
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                return func();
            }

            public override void Dispose()
            {
                _dispose?.Invoke();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                return new DefaultRedisSocket.TempRedisSocket(_redisSocket, null);
            }
            public override T2 AdapaterCall<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                return _cli.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    var rt = cmd.Read<T1>();
                    rt.IsErrorThrow = _cli._isThrowRedisSimpleError;
                    return parse(rt);
                });
            }
        }
    }
}
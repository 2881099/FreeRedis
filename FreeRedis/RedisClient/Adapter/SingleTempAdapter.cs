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
            readonly IRedisSocket _redisSocket;
            readonly Action _dispose;

            public SingleTempAdapter(RedisClient topOwner, IRedisSocket redisSocket, Action dispose)
            {
                UseType = UseType.SingleInside;
                TopOwner = topOwner;
                _redisSocket = redisSocket;
                _dispose = dispose;
            }

            public override void Dispose()
            {
                _dispose?.Invoke();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                return DefaultRedisSocket.CreateTempProxy(_redisSocket, null);
            }
            public override T2 AdapaterCall<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    var rt = cmd.Read<T1>();
                    rt.IsErrorThrow = TopOwner._isThrowRedisSimpleError;
                    return parse(rt);
                });
            }
        }
    }
}
using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            public override TValue AdapaterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    var rt = _redisSocket.Read(cmd._flagReadbytes);
                    if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                    rt.IsErrorThrow = TopOwner._isThrowRedisSimpleError;
                    return parse(rt);
                });
            }
#if net40
#else
            public override Task<TValue> AdapaterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                //Single socket not support Async Multiplexing
                return Task.FromResult(AdapaterCall(cmd, parse));
            }
#endif

        }
    }
}
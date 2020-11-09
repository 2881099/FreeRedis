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
        internal class SingleInsideAdapter : BaseAdapter
        {
            readonly IRedisSocket _redisSocket;

            public SingleInsideAdapter(RedisClient topOwner, RedisClient owner, string host, bool ssl, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected)
            {
                UseType = UseType.SingleInside;
                TopOwner = topOwner;
                _redisSocket = new DefaultRedisSocket(host, ssl);
                _redisSocket.Connected += (s, e) => connected?.Invoke(owner);
                _redisSocket.ConnectTimeout = connectTimeout;
                _redisSocket.ReceiveTimeout = receiveTimeout;
                _redisSocket.SendTimeout = sendTimeout;
            }

            public override void Dispose()
            {
                _redisSocket.Dispose();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                return DefaultRedisSocket.CreateTempProxy(_redisSocket, null);
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    var rt = _redisSocket.Read(cmd._flagReadbytes);
                    if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                    rt.IsErrorThrow = _isThrowRedisSimpleError;
                    if (rt.IsError) this.RedisSimpleError = new RedisServerException(rt.SimpleError);
                    return parse(rt);
                });
            }
#if isasync
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                //Single socket not support Async Multiplexing
                return Task.FromResult(AdapterCall(cmd, parse));
            }
#endif

            internal bool _isThrowRedisSimpleError { get; set; } = true;
            protected internal RedisServerException RedisSimpleError { get; private set; }
            protected internal IDisposable NoneRedisSimpleError()
            {
                var old_isThrowRedisSimpleError = _isThrowRedisSimpleError;
                _isThrowRedisSimpleError = false;
                return new TempDisposable(() =>
                {
                    _isThrowRedisSimpleError = old_isThrowRedisSimpleError;
                    RedisSimpleError = null;
                });
            }
        }
    }
}
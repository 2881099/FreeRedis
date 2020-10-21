using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        class SingleInsideAdapter : BaseAdapter
        {
            readonly IRedisSocket _redisSocket;

            public SingleInsideAdapter(RedisClient client, string host, bool ssl, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected)
            {
                UseType = UseType.SingleInside;
                var _redisSocket = new DefaultRedisSocket(host, ssl);
                _redisSocket.Connected += (s, e) => connected(client);
                _redisSocket.Client = client;
                _redisSocket.ConnectTimeout = connectTimeout;
                _redisSocket.ReceiveTimeout = receiveTimeout;
                _redisSocket.SendTimeout = sendTimeout;
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                return func();
            }

            public override void Dispose()
            {
                _redisSocket.Dispose();
            }
            public override void Reset()
            {
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                return new DefaultRedisSocket.TempRedisSocket(_redisSocket, null, null);
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
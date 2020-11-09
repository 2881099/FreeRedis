using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        protected internal enum UseType
        {
            Pooling,
            Cluster,
            Sentinel,
            SingleInside,
            SingleTemp,

            Pipeline,
            Transaction,
        }

        protected internal abstract partial class BaseAdapter
        {
            public static ThreadLocal<Random> _rnd = new ThreadLocal<Random>(() => new Random());
            public UseType UseType { get; protected set; }
            protected internal RedisClient TopOwner { get; internal set; }

            public abstract void Refersh(IRedisSocket redisSocket);
            public abstract IRedisSocket GetRedisSocket(CommandPacket cmd);
            public abstract void Dispose();

            public abstract TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse);

#if isasync
            public abstract Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse);
#endif

        }
    }
}

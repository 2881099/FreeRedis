using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FreeRedis
{
    partial class RedisClient
    {

        protected internal enum UseType { Pooling, Cluster, Sentinel, SingleInside, Pipeline, Transaction }

        protected internal abstract class BaseAdapter
        {
            public static ThreadLocal<Random> _rnd = new ThreadLocal<Random>(() => new Random());
            public UseType UseType { get; protected set; }

            public abstract IRedisSocket GetRedisSocket(CommandPacket cmd);
            public abstract void Dispose();

            public abstract T CheckSingle<T>(Func<T> func);
            public abstract T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse);
        }
    }
}

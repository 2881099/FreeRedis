using hiredis.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hiredis
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

            public abstract IRedisSocket GetRedisSocket(CommandPacket cmd);
            public abstract void Dispose();

            public abstract TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse);

#if net40
#else
            public abstract Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse);
#endif

        }
    }
}

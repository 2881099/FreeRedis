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
        // GetDatabase
        public DatabaseHook GetDatabase(int? index = null)
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Sentinel);
            var rds = Adapter.GetRedisSocket(null);
            DatabaseHook hook = null;
            try
            {
                var oldindex = rds.Database;
                hook = new DatabaseHook(new SingleTempAdapter(Adapter.TopOwner, rds, () =>
                {
                    try
                    {
                        if (index != null) hook.Select(oldindex);
                    }
                    finally
                    {
                        rds.Dispose();
                    }
                }));
                if (index != null) hook.Select(index.Value);
            }
            catch
            {
                rds.Dispose();
                throw;
            }
            return hook;
        }
        public class DatabaseHook : RedisClient
        {
            internal DatabaseHook(BaseAdapter adapter) : base(adapter) { }
        }

        public IRedisSocket GetTestRedisSocket() => Adapter.GetRedisSocket(null);

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
                    rt.IsErrorThrow = TopOwner._isThrowRedisSimpleError;
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

        }
    }
}
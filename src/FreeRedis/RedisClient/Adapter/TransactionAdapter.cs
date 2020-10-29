using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        internal class TransactionAdapter : BaseAdapter
        {
            IRedisSocket _redisSocket;
            readonly List<TransactionCommand> _commands;

            internal class TransactionCommand
            {
                public CommandPacket Command { get; set; }
                public Func<object, object> Parse { get; set; }
#if net40
#else
                public TaskCompletionSource<object> TaskCompletionSource { get; set; }
                bool TaskCompletionSourceIsTrySeted { get; set; }
                public void TrySetResult(object result, Exception exception)
                {
                    if (TaskCompletionSourceIsTrySeted) return;
                    TaskCompletionSourceIsTrySeted = true;
                    if (exception != null) TaskCompletionSource?.TrySetException(exception);
                    else TaskCompletionSource?.TrySetResult(result);
                }
                public void TrySetCanceled()
                {
                    if (TaskCompletionSourceIsTrySeted) return;
                    TaskCompletionSourceIsTrySeted = true;
                    TaskCompletionSource?.TrySetCanceled();
                }
#endif
            }

            public TransactionAdapter(RedisClient topOwner)
            {
                UseType = UseType.Transaction;
                TopOwner = topOwner;
                _commands = new List<TransactionCommand>();
            }

            public override void Dispose()
            {
                Discard();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                TryMulti();
                return DefaultRedisSocket.CreateTempProxy(_redisSocket, null);
            }
            public override TValue AdapaterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                TryMulti();
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    _redisSocket.Read(cmd._flagReadbytes).ThrowOrValue<TValue>(useDefaultValue: true);
                    _commands.Add(new TransactionCommand
                    {
                        Command = cmd,
                        Parse = obj => parse(new RedisResult(obj, true, RedisMessageType.SimpleString) { Encoding = _redisSocket.Encoding })
                    });
                    return default(TValue);
                });
            }
#if net40
#else
            async public override Task<TValue> AdapaterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                //Read with non byte[], Object deserialization is not supported
                //The value returned by the callback is null :
                //  tran.Get<Book>("key1").ContinueWith(t => t3 = t.Result)
                var tsc = new TaskCompletionSource<object>();
                TryMulti();
                TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    _redisSocket.Read(cmd._flagReadbytes).ThrowOrValue<TValue>(useDefaultValue: true);
                    _commands.Add(new TransactionCommand
                    {
                        Command = cmd,
                        Parse = obj => parse(new RedisResult(obj, true, RedisMessageType.SimpleString) { Encoding = _redisSocket.Encoding }),
                        TaskCompletionSource = tsc
                    });
                    return default(TValue);
                });
                var ret = await tsc.Task;
                return (TValue)ret;
            }
#endif

            object SelfCall(CommandPacket cmd)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    return _redisSocket.Read(false).ThrowOrValue();
                });
            }
            public void TryMulti()
            {
                if (_redisSocket == null)
                {
                    _redisSocket = TopOwner.Adapter.GetRedisSocket(null);
                    SelfCall("MULTI");
                }
            }
            void TryReset()
            {
                if (_redisSocket == null) return;
#if net40
#else
                for (var a = 0; a < _commands.Count; a++)
                    _commands[a].TrySetCanceled();
#endif
                _commands.Clear();
                _redisSocket?.Dispose();
                _redisSocket = null;
            }
            public void Discard()
            {
                if (_redisSocket == null) return;
                SelfCall("DISCARD");
                TryReset();
            }
            public object[] Exec()
            {
                if (_redisSocket == null) return new object[0];
                try
                {
                    var ret = SelfCall("EXEC") as object[];

                    var retparsed = new object[ret.Length];
                    for (var a = 0; a < ret.Length; a++)
                    {
                        retparsed[a] = _commands[a].Parse(ret[a]);
#if net40
#else
                        _commands[a].TrySetResult(retparsed[a], null); //tryset Async
#endif
                    }
                    return retparsed;
                }
                finally
                {
                    TryReset();
                }
            }
            public void UnWatch()
            {
                if (_redisSocket == null) return;
                SelfCall("UNWATCH");
            }
            public void Watch(params string[] keys)
            {
                if (_redisSocket == null) return;
                SelfCall("WATCH".Input(keys).FlagKey(keys));
            }
        }
    }
}
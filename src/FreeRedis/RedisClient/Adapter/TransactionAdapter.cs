using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                public object Result { get; set; }
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
            public override TValue AdapaterCall<TReadTextOrStream, TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                TryMulti();
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    _redisSocket.Read(typeof(TReadTextOrStream) == typeof(byte[])).ThrowOrValue<TValue>(useDefaultValue: true);
                    _commands.Add(new TransactionCommand
                    {
                        Command = cmd,
                        Parse = obj => parse(new RedisResult(obj, true, RedisMessageType.SimpleString) { Encoding = _redisSocket.Encoding })
                    });
                    return default(TValue);
                });
            }

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
            public void Discard()
            {
                if (_redisSocket == null) return;
                SelfCall("DISCARD");
                _commands.Clear();
                _redisSocket?.Dispose();
                _redisSocket = null;
            }
            public object[] Exec()
            {
                if (_redisSocket == null) return new object[0];
                try
                {
                    var ret = SelfCall("EXEC") as object[];

                    for (var a = 0; a < ret.Length; a++)
                        _commands[a].Result = _commands[a].Parse(ret[a]);
                    return _commands.Select(a => a.Result).ToArray();
                }
                finally
                {
                    _commands.Clear();
                    _redisSocket?.Dispose();
                    _redisSocket = null;
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
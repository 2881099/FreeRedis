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
        class TransactionAdapter : BaseAdapter
        {
            readonly RedisClient _cli;
            IRedisSocket _redisSocket;
            readonly List<TransactionCommand> _commands;

            internal class TransactionCommand
            {
                public CommandPacket Command { get; set; }
                public Func<object, object> Parse { get; set; }
                public object Result { get; set; }
            }

            public TransactionAdapter(RedisClient cli)
            {
                UseType = UseType.Transaction;
                _cli = cli;
                _commands = new List<TransactionCommand>();
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                return func();
            }

            public override void Dispose()
            {
                Discard();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                TryMulti();
                return new DefaultRedisSocket.TempRedisSocket(_redisSocket, null, null);
            }
            public override T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                TryMulti();
                if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                _redisSocket.Write(cmd);
                cmd.Read<T1>().ThrowOrValue();
                cmd._readed = false; //exec 还需要再读一次
                _commands.Add(new TransactionCommand
                {
                    Command = cmd,
                    Parse = obj => parse(new RedisResult<T1>(obj.ConvertTo<T1>(), null, true, RedisMessageType.SimpleString) { Encoding = _redisSocket.Encoding })
                });
                return default(T2);
            }

            object CallTransactionCommand(CommandPacket cmd)
            {
                _redisSocket.Write(cmd);
                return cmd.Read<object>().ThrowOrValue();
            }
            public void TryMulti()
            {
                if (_redisSocket == null)
                {
                    _redisSocket = _cli._adapter.GetRedisSocket(null);
                    if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                    CallTransactionCommand("MULTI");
                }
            }
            public void Discard()
            {
                if (_redisSocket == null) return;
                CallTransactionCommand("DISCARD");
                _commands.Clear();
                _redisSocket?.Dispose();
                _redisSocket = null;
            }
            public object[] Exec()
            {
                if (_redisSocket == null) return new object[0];
                try
                {
                    if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                    var ret = CallTransactionCommand("EXEC") as List<object>;

                    for (var a = 0; a < ret.Count; a++)
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
                CallTransactionCommand("UNWATCH");
            }
            public void Watch(params string[] keys)
            {
                if (_redisSocket == null) return;
                CallTransactionCommand("WATCH".Input(keys).FlagKey(keys));
            }
        }
    }
}
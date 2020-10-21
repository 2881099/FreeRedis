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
                public Func<TransactionCommand, object> Parse { get; set; }
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
                if (_redisSocket == null)
                    _redisSocket = _cli._adapter.GetRedisSocket(null);

                return new DefaultRedisSocket.TempRedisSocket(_redisSocket, null, null);
            }
            public override T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                if (_redisSocket == null)
                    _redisSocket = _cli._adapter.GetRedisSocket(null);

                if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                _redisSocket.Write(cmd);
                cmd.Read<string>().ThrowOrValue();
                cmd._readed = false; //exec 还需要再读一次
                _commands.Add(new TransactionCommand
                {
                    Command = cmd,
                    Parse = pc =>
                    {
                        var rt = pc.Command.Read<T1>();
                        rt.IsErrorThrow = _cli._isThrowRedisSimpleError;
                        return parse(rt);
                    }
                });
                return default(T2);
            }


            public void Discard()
            {
                if (_redisSocket == null) return;
                Call<string, string>("DISCARD", rt => rt.ThrowOrValue());
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
                    _redisSocket.Write("EXEC");

                    switch (UseType)
                    {
                        case UseType.Pooling: break;
                        case UseType.Cluster: return ClusterExec();
                        case UseType.Sentinel:
                        case UseType.SingleInside: break;
                    }

                    Exec(_redisSocket, _commands);
                    return _commands.Select(a => a.Result).ToArray();
                }
                finally
                {
                    _commands.Clear();
                }

                object[] ClusterExec()
                {
                    throw new NotSupportedException();
                }
            }
            static void Exec(IRedisSocket rds, IEnumerable<TransactionCommand> cmds)
            {
                var err = new List<TransactionCommand>();
                var ms = new MemoryStream();

                try
                {
                    foreach (var pc in cmds)
                    {
                        pc.Result = pc.Parse(pc);
                        if (pc.Command.ReadResult.IsError) err.Add(pc);
                    }
                }
                finally
                {
                    ms.Close();
                    ms.Dispose();
                    rds?.Dispose();
                }

                if (err.Any())
                {
                    var sb = new StringBuilder();
                    for (var a = 0; a < err.Count; a++)
                    {
                        var cmd = err[a].Command;
                        if (a > 0) sb.Append("\r\n");
                        sb.Append(cmd.ReadResult.SimpleError).Append(" {").Append(cmd.ToString()).Append("}");
                    }
                    throw new RedisException(sb.ToString());
                }
            }

            public void UnWatch()
            {
                if (_redisSocket == null) return;
                Call<string, string>("UNWATCH", rt => rt.ThrowOrValue());
            }
            public void Watch(params string[] keys)
            {
                if (_redisSocket == null) return;
                Call<string, string>("WATCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
            }
        }
    }
}
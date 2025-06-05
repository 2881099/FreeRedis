using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                hook = new DatabaseHook(new SingleTempAdapter(Adapter.TopOwner, rds, index));
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

        class SingleTempAdapter : BaseAdapter
        {
            readonly IRedisSocket _redisSocket;
            readonly int? _index;
            readonly int _oldIndex;

            public SingleTempAdapter(RedisClient topOwner, IRedisSocket redisSocket, int? index)
            {
                UseType = UseType.SingleInside;
                TopOwner = topOwner;
                _redisSocket = redisSocket;
                _index = index;
                _oldIndex = redisSocket.Database;
            }

            public override void Dispose()
            {
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
                if (_index == null || cmd._command == "QUIT")
                    return TopOwner.LogCall(cmd, () =>
                    {
                        _redisSocket.Write(cmd);
                        var rt = _redisSocket.Read(cmd);
                        if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                        return parse(rt);
                    });

                var cmds = new[]
                {
                    new PipelineCommand
                    {
                        Command = "SELECT".Input(_index.Value),
                        Parse = rt => rt.ThrowOrValue()
                    },
                    new PipelineCommand
                    {
                        Command = cmd,
                        Parse = rt => parse(rt)
                    },
                    new PipelineCommand
                    {
                        Command = "SELECT".Input(_oldIndex),
                        Parse = rt => rt.ThrowOrValue()
                    },
                };
                return TopOwner.LogCall(cmd, () =>
                {
                    cmds[1].Command.WriteTarget = $"{_redisSocket.Host}/{_index}";
                    PipelineAdapter.EndPipe(_redisSocket, cmds);
                    return (TValue)cmds[1].Result;
                });
            }
#if isasync
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                if (_index == null || cmd._command == "QUIT")
                    return TopOwner.LogCallAsync(cmd, async () =>
                    {
                        await _redisSocket.WriteAsync(cmd);
                        var rt = await _redisSocket.ReadAsync(cmd);
                        if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                        return parse(rt);
                    });

                var cmds = new[]
                {
                    new PipelineCommand
                    {
                        Command = "SELECT".Input(_index.Value),
                        Parse = rt => rt.ThrowOrValue()
                    },
                    new PipelineCommand
                    {
                        Command = cmd,
                        Parse = rt => parse(rt)
                    },
                    new PipelineCommand
                    {
                        Command = "SELECT".Input(_oldIndex),
                        Parse = rt => rt.ThrowOrValue()
                    },
                };
                return TopOwner.LogCallAsync(cmd, async () =>
                {
                    cmds[1].Command.WriteTarget = $"{_redisSocket.Host}/{_index}";
                    PipelineAdapter.EndPipe(_redisSocket, cmds);
                    await Task.Yield();
                    return (TValue)cmds[1].Result;
                });
            }
#endif

        }
    }
}
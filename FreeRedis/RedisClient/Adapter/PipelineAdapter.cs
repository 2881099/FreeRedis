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
        class PipelineAdapter : BaseAdapter
        {
            protected readonly RedisClient _cli;
            protected readonly List<PipelineCommand> _commands;

            internal class PipelineCommand
            {
                public CommandPacket Command { get; set; }
                public Func<PipelineCommand, object> Parse { get; set; }
                public object Result { get; set; }
            }

            public PipelineAdapter(RedisClient cli)
            {
                UseType = UseType.Pipeline;
                _cli = cli;
                _commands = new List<PipelineCommand>();
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                return func();
            }

            public override void Dispose()
            {
                _commands.Clear();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                throw new Exception($"RedisClient: Method cannot be used in {UseType} mode.");
            }
            public override T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                _commands.Add(new PipelineCommand
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

            public object[] EndPipe()
            {
                if (_commands.Any() == false) return new object[0];

                try
                {
                    switch (UseType)
                    {
                        case UseType.Pooling: break;
                        case UseType.Cluster: return ClusterEndPipe();
                        case UseType.Sentinel:
                        case UseType.SingleInside: break;
                    }

                    EndPipe(_cli._adapter.GetRedisSocket(null), _commands);
                    return _commands.Select(a => a.Result).ToArray();
                }
                finally
                {
                    _commands.Clear();
                }

                object[] ClusterEndPipe()
                {
                    throw new NotSupportedException();
                }
            }

            static void EndPipe(IRedisSocket rds, IEnumerable<PipelineCommand> cmds)
            {
                var err = new List<PipelineCommand>();
                var ms = new MemoryStream();

                try
                {
                    foreach (var cmd in cmds)
                        RespHelper.Write(ms, rds.Encoding, cmd.Command, rds.Protocol);

                    if (rds.IsConnected == false) rds.Connect();
                    ms.CopyTo(rds.Stream);

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
        }
    }
}
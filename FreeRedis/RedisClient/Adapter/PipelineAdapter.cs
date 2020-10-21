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
                public RedisResult RedisResult { get; set; }
                public object Result { get; set; }
            }

            public PipelineAdapter(RedisClient client)
            {
                UseType = UseType.Pipeline;
                _cli = client;
                _commands = new List<PipelineCommand>();
            }

            public override T CheckSingle<T>(Func<T> func)
            {
                return func();
            }

            public override void Dispose()
            {
            }
            public override void Reset()
            {
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
                        pc.RedisResult = rt;
                        return parse(rt);
                    }
                });
                return default(T2);
            }

            public object[] EndPipe()
            {
                if (_commands.Any() == false) return new object[0];
                switch (UseType)
                {
                    case UseType.Pooling: break;
                    case UseType.Cluster: return ClusterEndPipe();
                    case UseType.Sentinel:
                    case UseType.SingleInside: break;
                }

                EndPipe(_cli._adapter.GetRedisSocket(null), _commands);
                return _commands.Select(a => a.Result).ToArray();

                object[] ClusterEndPipe()
                {
                    throw new NotSupportedException();
                }
            }

            static void EndPipe(IRedisSocket rds, List<PipelineCommand> cmds)
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
                        var result = pc.Command.Read<object>();
                        pc.Result = pc.Parse(pc);
                        if (pc.RedisResult.IsError) err.Add(pc);
                    }
                }
                finally
                {
                    cmds.Clear();
                    ms.Close();
                    ms.Dispose();
                    rds?.Dispose();
                }

                if (err.Any())
                {
                    var sb = new StringBuilder();
                    for (var a = 0; a < err.Count; a++)
                    {
                        if (a > 0) sb.Append("\r\n");
                        sb.Append(err[a].RedisResult.SimpleError).Append(" {");
                        List<object> cmdlst = err[a].Command;
                        for (var b = 0; b < cmdlst.Count; b++)
                        {
                            if (b > 0) sb.Append(" ");
                            var tmpstr = cmdlst[b].ToInvariantCultureToString().Replace("\r\n", "\\r\\n");
                            if (tmpstr.Length > 32) tmpstr = $"{tmpstr.Substring(0, 32).Trim()}..";
                            sb.Append(tmpstr);
                        }
                        sb.Append("}");
                    }
                    throw new RedisException(sb.ToString());
                }
            }
        }
    }
}
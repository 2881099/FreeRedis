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
        internal class PipelineAdapter : BaseAdapter
        {
            readonly List<PipelineCommand> _commands;

            internal class PipelineCommand
            {
                public CommandPacket Command { get; set; }
                public Func<RedisResult, object> Parse { get; set; }
                public bool IsBytes { get; set; }
                public object Result { get; set; }
            }

            public PipelineAdapter(RedisClient topOwner)
            {
                UseType = UseType.Pipeline;
                TopOwner = topOwner;
                _commands = new List<PipelineCommand>();
            }

            public override void Dispose()
            {
                _commands.Clear();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                throw new Exception($"RedisClient: Method cannot be used in {UseType} mode.");
            }
            public override TValue AdapaterCall<TReadTextOrStream, TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                _commands.Add(new PipelineCommand
                {
                    Command = cmd,
                    Parse = rt => parse(rt),
                    IsBytes = typeof(TReadTextOrStream) == typeof(byte[])
                });
                TopOwner.OnNotice(new NoticeEventArgs(NoticeType.Call, null, $"Pipeline > {cmd}", null));
                return default(TValue);
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
                        case UseType.SingleTemp: break;
                    }

                    CommandPacket epcmd = "EndPipe";
                    return TopOwner.LogCall(epcmd, () =>
                    {
                        using (var rds = TopOwner.Adapter.GetRedisSocket(null))
                        {
                            EndPipe(rds, _commands);
                        }
                        return _commands.Select(a => a.Result).ToArray();
                    });
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
                    var respWriter = new RespHelper.Resp3Writer(ms, rds.Encoding, rds.Protocol);
                    foreach (var cmd in cmds)
                        respWriter.WriteCommand(cmd.Command);

                    if (rds.IsConnected == false) rds.Connect();
                    ms.Position = 0;
                    ms.CopyTo(rds.Stream);
                    
                    foreach (var pc in cmds)
                    {
                        var rt = rds.Read(pc.IsBytes);
                        pc.Result = pc.Parse(rt);
                        if (pc.Command.ReadResult.IsError) err.Add(pc);
                    }
                }
                finally
                {
                    ms.Close();
                    ms.Dispose();
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
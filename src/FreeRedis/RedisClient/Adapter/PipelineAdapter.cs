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
        // Pipeline
        public PipelineHook StartPipe()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Cluster, UseType.Sentinel, UseType.SingleInside, UseType.SingleTemp);
            return new PipelineHook(new PipelineAdapter(Adapter));
        }
        public class PipelineHook : RedisClient
        {
            internal PipelineHook(BaseAdapter adapter) : base(adapter) { }
            public object[] EndPipe() => (Adapter as PipelineAdapter).EndPipe();

            ~PipelineHook()
            {
                (Adapter as PipelineAdapter).Dispose();
            }
        }

        class PipelineAdapter : BaseAdapter
        {
            readonly List<PipelineCommand> _commands;
            readonly BaseAdapter _baseAdapter;

            internal class PipelineCommand
            {
                public CommandPacket Command { get; set; }
                public Func<RedisResult, object> Parse { get; set; }
                public bool IsBytes { get; set; }
                public RedisResult RedisResult { get; set; }
                public object Result { get; set; }
#if isasync
                public TaskCompletionSource<object> TaskCompletionSource { get; set; }
                bool TaskCompletionSourceIsTrySeted { get; set; }
                public void TrySetResult(object result, Exception exception)
                {
                    if (TaskCompletionSource == null) return;
                    if (TaskCompletionSourceIsTrySeted) return;
                    TaskCompletionSourceIsTrySeted = true;
                    if (exception != null) TaskCompletionSource.TrySetException(exception);
                    else TaskCompletionSource.TrySetResult(result);
                }
                public void TrySetCanceled()
                {
                    if (TaskCompletionSource == null) return;
                    if (TaskCompletionSourceIsTrySeted) return;
                    TaskCompletionSourceIsTrySeted = true;
                    TaskCompletionSource.TrySetCanceled();
                }
#endif
            }

            public PipelineAdapter(BaseAdapter baseAdapter)
            {
                UseType = UseType.Pipeline;
                TopOwner = baseAdapter.TopOwner;
                _baseAdapter = baseAdapter;
                _commands = new List<PipelineCommand>();
            }

            public override void Dispose()
            {
#if isasync
                for (var a = 0; a < _commands.Count; a++)
                    _commands[a].TrySetCanceled();
#endif
                _commands.Clear();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                throw new RedisClientException($"RedisClient: Method cannot be used in {UseType} mode.");
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                _commands.Add(new PipelineCommand
                {
                    Command = cmd,
                    Parse = rt => parse(rt),
                    IsBytes = cmd._flagReadbytes
                });
                TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Call, null, $"{"Pipeline".PadRight(21)} > {cmd}", null));
                return default(TValue);
            }
#if isasync
            async public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                var tsc = new TaskCompletionSource<object>();
                _commands.Add(new PipelineCommand
                {
                    Command = cmd,
                    Parse = rt => parse(rt),
                    IsBytes = cmd._flagReadbytes,
                    TaskCompletionSource = tsc
                });
                TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Call, null, $"{"Pipeline".PadRight(21)} > {cmd}", null));
                var ret = await tsc.Task;
                return (TValue)ret;
            }
#endif

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
                        using (var rds = _baseAdapter.GetRedisSocket(null))
                        {
                            EndPipe(rds, _commands);
                        }
                        return _commands.Select(a => a.Result).ToArray();
                    });
                }
                finally
                {
                    Dispose();
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
                        pc.RedisResult = rds.Read(pc.Command);
                        pc.Result = pc.Parse(pc.RedisResult);
                        if (pc.RedisResult.IsError) err.Add(pc);
#if isasync
                        pc.TrySetResult(pc.Result, pc.RedisResult.IsError ? new RedisServerException(pc.RedisResult.SimpleError) : null);
#endif
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
                        sb.Append(err[a].RedisResult.SimpleError).Append(" {").Append(cmd.ToString()).Append("}");
                    }
                    throw new RedisServerException(sb.ToString());
                }
            }
        }
    }
}

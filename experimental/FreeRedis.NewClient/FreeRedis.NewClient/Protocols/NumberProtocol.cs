using System.Buffers;
using System.Text;

namespace FreeRedis.NewClient.Protocols
{
    internal static class NumberProtocol
    {
        public const byte TAIL = 10;
        public const byte NUMBER_HEAD = 58;
        private static readonly Encoding _utf8;
        static NumberProtocol()
        {
            _utf8 = Encoding.UTF8;
        }
        internal static ProtocolContinueResult HandleBytes(TaskCompletionSource<long> task, ref SequenceReader<byte> reader)
        {
#if DEBUG
            FreeRedisTracer.CurrentStep = $"整数协议解析中: 当前需解析的长度为:{reader.Length}!";
#endif
            //try
            //{
                if (reader.TryReadTo(out ReadOnlySpan<byte> span, TAIL, true))
                {
                    if (span[0] == NUMBER_HEAD)
                    {
                        task.SetResult(Convert.ToInt64(_utf8.GetString(span[1..^1])));
                    }
                    else
                    {
                        task.SetException(new Exception(_utf8.GetString(span)));
                    }
                    return ProtocolContinueResult.Completed;
                }
                return ProtocolContinueResult.Wait;
            //}
            //catch (Exception ex)
            //{

                //throw ex;
            //}
            
        }
    }
}

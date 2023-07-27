using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace FreeRedis.NewClient.Protocols
{
    internal static class StateProtocol
    {
        public const byte TAIL = 10;
        public const byte OK_HEAD = 43;
        private static readonly Encoding _utf8;
        static StateProtocol()
        {
            _utf8 = Encoding.UTF8;
        }
        internal static ProtocolContinueResult HandleBytes(TaskCompletionSource<bool> task, ref SequenceReader<byte> reader)
        {
#if DEBUG
            FreeRedisTracer.CurrentStep = $"状态协议解析中: 当前需解析的长度为:{reader.Length}!";
#endif
            //try
            //{
            if (reader.CurrentSpan[reader.CurrentSpanIndex].Equals(OK_HEAD))
            {

                reader.Advance(5);
                task.SetResult(true);

            }
            else if (reader.TryReadTo(out ReadOnlySpan<byte> sequence, TAIL, true))
            {
                task.SetException(new Exception(_utf8.GetString(sequence)));
            }
            else
            {
                throw new Exception("出现意外!");
            }
            return ProtocolContinueResult.Completed;
            //}
            //catch (Exception ex)
            //{
            //throw ex;
            //}
        }
    }
}

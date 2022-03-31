using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    public sealed class FlushProtocol : IRedisProtocal<bool>
    {
        internal static readonly byte[] FlushCommandBuffer;
        static FlushProtocol()
        {
            FlushCommandBuffer = Encoding.UTF8.GetBytes($"FLUSHDB\r\n");
        }
        public FlushProtocol(Action<string>? logger) : base(logger)
        {

        }

        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(false);
        }

        //public override void WriteBuffer(PipeWriter bufferWriter)
        //{
        //    bufferWriter.Write(_flushCommandBuffer);
        //}

        /// <summary>
        /// 处理协议
        /// </summary>
        /// <param name="recvBytes"></param>
        /// <param name="offset"></param>
        /// <returns>false:继续使用当前实例处理下一个数据流</returns>
        protected override ProtocolContinueResult HandleOkBytes(ref SequenceReader<byte> recvReader)
        {
            if (recvReader.IsNext(OK_HEAD))
            {
                if (recvReader.TryReadTo(out ReadOnlySpan<byte> _, TAIL, true))
                {
                    Task.SetResult(true);
                    return ProtocolContinueResult.Completed;
                }
                return ProtocolContinueResult.Wait;
            }
            throw new Exception($"{this.GetType()}协议未解析到标准协议头!下一个协议字段为:{recvReader.UnreadSpan[0]}");

        }
    }
}

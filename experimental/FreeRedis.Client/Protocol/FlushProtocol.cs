using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    internal class FlushProtocol : IRedisProtocal<bool>
    {
        private static readonly byte[] _flushCommandBuffer;
        static FlushProtocol()
        {
            _flushCommandBuffer = Encoding.UTF8.GetBytes($"FLUSHDB\r\n");
        }
        public FlushProtocol(Action<string>? logger) : base(logger)
        {
            Command = $"FLUSHDB\r\n";
        }

        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(false);
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.Write(_flushCommandBuffer);
            bufferWriter.FlushAsync();
        }

        /// <summary>
        /// 处理协议
        /// </summary>
        /// <param name="recvBytes"></param>
        /// <param name="offset"></param>
        /// <returns>false:继续使用当前实例处理下一个数据流</returns>
        protected override ProtocolContinueResult HandleOkBytes(ref SequenceReader<byte> recvReader)
        {
            var span = recvReader.UnreadSpan;
            if (span[0] == OK_HEAD)
            {
                var position = span.IndexOf(TAIL);
                if (position != -1)
                {
                    Task.SetResult(true);
                    recvReader.Advance(position + 1);
                    return ProtocolContinueResult.Completed;
                }
                return ProtocolContinueResult.Wait;
            }
            throw new Exception($"{this.GetType()}协议未解析到标准协议头!下一个协议字段为:{recvReader.UnreadSpan[0]}");

        }
    }
}

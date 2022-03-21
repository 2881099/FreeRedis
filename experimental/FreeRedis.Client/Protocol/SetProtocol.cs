using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    internal class SetProtocol : IRedisProtocal<bool>
    {
        private static readonly byte[] _fixedBuffer;

        static SetProtocol()
        {
            _fixedBuffer = Encoding.UTF8.GetBytes("*3\r\n$3\r\nSET\r\n$");
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.Write(_fixedBuffer);
            Utf8Encoder.Convert(Command, bufferWriter, false, out _, out _);
            bufferWriter.Write(SplitField);
            bufferWriter.FlushAsync();
        }
        public SetProtocol(string key, string value, Action<string>? logger) : base(logger)
        {
            Command = $"{key.Length}\r\n{key}\r\n${value.Length}\r\n{value}";
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

        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(false);
        }

    }
}

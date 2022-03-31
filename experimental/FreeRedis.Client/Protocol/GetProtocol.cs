using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    public sealed class GetProtocol : IRedisProtocal<string?>
    {
        public GetProtocol(string key, Action<string>? logger) : base(logger)
        {
            Command = $"*2\r\n$3\r\nGET\r\n${key.Length}\r\n{key}\r\n";
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.WriteUtf8String(Command);
        }

        private long bufferLength;
        /// <summary>
        /// 处理协议
        /// </summary>
        /// <param name="recvBytes"></param>
        /// <param name="offset"></param>
        /// <returns>false:继续使用当前实例处理下一个数据流</returns>
        protected override ProtocolContinueResult HandleOkBytes(ref SequenceReader<byte> recvReader)
        {
            if (bufferLength > 0)
            {
                return HandleSerializer(ref recvReader);
            }
            else
            {
                if (recvReader.IsNext(OK_DATA, true))
                {
                    //获取完整的长度字段
                    if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
                    {
                        var lengthBytes = requestLine[0..^1];
                        //如果是 -1 则返回空
                        if (lengthBytes[0] == ERROR_HEAD)
                        {
                            SetErrorDefaultResult();
                            return ProtocolContinueResult.Completed;
                        }
                        else
                        {
                            bufferLength = lengthBytes[0] - 48;
                            if (lengthBytes.Length > 1)
                            {
                                for (int i = 1; i < lengthBytes.Length; i += 1)
                                {
                                    bufferLength *= 10;
                                    bufferLength += lengthBytes[i] - 48;
                                }
                            }
                            return HandleSerializer(ref recvReader);
                        }
                    }

                    return ProtocolContinueResult.Wait;

                }
            }
            throw new Exception($"{this.GetType()}协议未解析到协议头!");
        }
        public string? StringResult;
        private ProtocolContinueResult HandleSerializer(ref SequenceReader<byte> recvReader)
        {
            if (recvReader.UnreadSequence.Length > bufferLength + 2)
            {
                var bytes = recvReader.UnreadSequence.Slice(0, bufferLength);
                Task.SetResult(Encoding.UTF8.GetString(in bytes));
                recvReader.Advance(bufferLength + 2);
                return ProtocolContinueResult.Completed;
            }
            else
            {
                return ProtocolContinueResult.Wait;
            }
        }
        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(null);
        }

    }
}

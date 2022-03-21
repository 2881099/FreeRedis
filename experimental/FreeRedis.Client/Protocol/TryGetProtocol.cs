using FreeRedis.Client.Protocol.Serialization;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    internal class TryGetProtocol<T> : IRedisProtocal<TryResult<T>>
    {
        private static readonly byte[] _fixedBuffer;
        static TryGetProtocol()
        {
            _fixedBuffer = Encoding.UTF8.GetBytes("*2\r\n$3\r\nGET\r\n$");
        }
        public TryGetProtocol(string key, Action<string>? logger) : base(logger)
        {
            Command = $"{key.Length}\r\n{key}";
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.Write(_fixedBuffer);
            Utf8Encoder.Convert(Command, bufferWriter, false, out _, out _);
            bufferWriter.Write(SplitField);
            bufferWriter.FlushAsync();
        }

        private int bufferLength;
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
                return HanadleSerializer(ref recvReader);
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
                            return HanadleSerializer(ref recvReader);
                        }
                    }

                    return ProtocolContinueResult.Wait;

                }
            }
            throw new Exception($"{this.GetType()}协议未解析到协议头!");
        }

        private ProtocolContinueResult HanadleSerializer(ref SequenceReader<byte> recvReader)
        {
            var span = recvReader.UnreadSpan;
            if (span.Length >= bufferLength + 2)
            {
                var result = ProtocolSerializer<T>.ReadFunc(span[..bufferLength]);
                TryResult<T> tryResult = new TryResult<T>(true, result);
                recvReader.Advance(bufferLength + 2);
                Task.SetResult(tryResult);
                return ProtocolContinueResult.Completed;
            }
            else
            {
                return ProtocolContinueResult.Wait;
            }
        }
        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(new TryResult<T>(false, default));
        }

    }
}


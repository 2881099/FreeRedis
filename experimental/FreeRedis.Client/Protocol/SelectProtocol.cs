using System.Buffers;
using System.IO.Pipelines;

namespace FreeRedis.Client.Protocol
{
    public sealed class SelectProtocol : IRedisProtocal<bool>
    {
        //private static readonly byte[] _fixedBuffer;

        //static SelectProtocol()
        //{
        //    _fixedBuffer = Encoding.UTF8.GetBytes("SELECT ");
        //}




        public SelectProtocol(int dbIndex,Action<string>? logger) : base(logger)
        {
            Command = $"SELECT {dbIndex}\r\n";
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.WriteUtf8String(Command);
        }

        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(false);
        }

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
                if (recvReader.TryReadTo(out ReadOnlySpan<byte> data, TAIL, true))
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

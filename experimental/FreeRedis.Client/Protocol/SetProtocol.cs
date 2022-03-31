using System.Buffers;
using System.IO.Pipelines;

namespace FreeRedis.Client.Protocol
{
    public sealed class SetProtocol : IRedisProtocal<bool>
    {
        //static SetProtocol()
        //{
        //    System.Threading.Tasks.Task.Run(() => {

        //        while (true)
        //        {
        //            Thread.Sleep(5000);
        //            Console.WriteLine("已处理:"+count);
        //        }
            
        //    });
        //}
        public SetProtocol(string key, string value, Action<string>? logger) : base(logger)
        {
            Command = $"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n";
        }

        public override void WriteBuffer(PipeWriter bufferWriter)
        {
            bufferWriter.WriteUtf8String(Command);
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
                if (recvReader.TryReadTo(out ReadOnlySpan<byte> _, TAIL, true))
                {
                    Task.SetResult(true);
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

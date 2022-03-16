using System.Buffers;
using System.Text;

namespace FreeRedis.Client.Protocol
{
    internal class AuthProtocol : IRedisProtocal<bool>
    {
        static int a = 1;
        public AuthProtocol(string password, Action<string>? logger) : base(logger)
        {
            ReadBuffer = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
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
            if (recvReader.IsNext(OK_HEAD, false))
            {
                if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
                {
                    Task.SetResult(true);
                    Console.WriteLine(Encoding.UTF8.GetString(requestLine));
                    return ProtocolContinueResult.Completed;
                }
                return ProtocolContinueResult.Wait;
            }
            throw new Exception($"{this.GetType()}协议未解析到标准协议头!");

        }
    }
}

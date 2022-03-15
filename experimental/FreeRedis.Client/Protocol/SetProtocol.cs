using System.Buffers;
using System.Text;

namespace FreeRedis.Client.Protocal
{
    internal class SetProtocol : IRedisProtocal<bool>
    {
        public SetProtocol(string key, string value, Action<string>? logger) : base(logger)
        {
            ReadBuffer = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
        }
        /// <summary>
        /// 处理协议
        /// </summary>
        /// <param name="recvBytes"></param>
        /// <param name="offset"></param>
        /// <returns>false:继续使用当前实例处理下一个数据流</returns>
        protected override bool HandleOkBytes(ref SequenceReader<byte> recvReader)
        {
            if (recvReader.IsNext(OK_HEAD, true))
            {
                Task.SetResult(true);
                if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
                {
                    Console.WriteLine(Encoding.UTF8.GetString(requestLine));
                    return true;
                }
                else
                {
                    //交给默认的"OK"粘包处理逻辑
                    this.PostToAfterOk();
                    //粘包处理(前段)
                    return false;
                }

            }
            throw new Exception($"{this.GetType()}协议未解析到协议头!");

        }

        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(false);
        }
    }
}

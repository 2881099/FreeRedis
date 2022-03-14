using System.Text;

namespace FreeRedis.Client.Protocal
{
    internal class AuthProtocal : IRedisProtocal<bool>
    {
        public AuthProtocal(string password)
        {
            ReadBuffer = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
        }

        /// <summary>
        /// 处理协议
        /// </summary>
        /// <param name="recvBytes"></param>
        /// <param name="offset"></param>
        /// <returns>false:继续使用当前实例处理下一个数据流</returns>
        public override bool GetInstanceFromBytes(in ReadOnlyMemory<byte> recvBytes, ref int offset)
        {
            //Todo handle recvBytes
            //var findChar = recvBytes.Span.IndexOf(xx);
            //if (findChar != -1)
            //{
                //offset += findChar;
                //xxxxx
            //}
            //result eg. Task.SetResult(true/false);

            //完成任务并设置结果
            Task.SetResult(true);
            //无需继续处理数据流
            return true;
            //当前数据段不完整需要继续传递流进来处理
            return false;
        }
    }
}

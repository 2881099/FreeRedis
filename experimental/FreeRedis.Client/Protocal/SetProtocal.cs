using System.Text;

namespace FreeRedis.Client.Protocal
{
    internal class SetProtocal : IRedisProtocal<bool>
    {
        public SetProtocal(string key, string value)
        {
            ReadBuffer = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
        }
        public override bool GetInstanceFromBytes(in ReadOnlyMemory<byte> recvBytes, ref int offset)
        {
            throw new NotImplementedException();
        }
    }
}

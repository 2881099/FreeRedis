using FreeRedis.Engine;
using FreeRedis.NewClient.Protocols;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace FreeRedis.NewClient.Client
{
    public sealed partial class FreeRedisClient
    {
        private static readonly Encoding _utf8;
        private static readonly Encoder _utf8_encoder;

        private static readonly byte[] _flush_command_buffer;
        private static readonly byte[] _exist_command_buffer;
        private static readonly int _exist_command_length;
        private static readonly byte[] _set_header_buffer;
        private static readonly int _set_command_length;
        private static readonly int _set_header_length;

        internal readonly struct UTF8KeyByte
        {
            public UTF8KeyByte(byte[] values, int length, int realLength)
            {
                Values = values;
                Length = length;
                ValueLength = realLength;
            }
            public readonly byte[] Values;
            public readonly int Length;
            public readonly int ValueLength;
        }

        private static UTF8KeyByte[] _protocolFillArray;

        static FreeRedisClient()
        {
            _utf8 = Encoding.UTF8;
            _utf8_encoder = _utf8.GetEncoder();
            _protocolFillArray = new UTF8KeyByte[10001];
            for (int i = 0; i < 10001; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                if (i < 10)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 1, i);
                }
                else if (i < 100)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 2, i);
                }
                else if (i < 1000)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 3, i);
                }
            }
            _flush_command_buffer = _utf8.GetBytes($"FLUSHDB\r\n");
            _exist_command_buffer = _utf8.GetBytes($"EXISTS ");
            _exist_command_length = _exist_command_buffer.Length;
            _set_header_buffer = _utf8.GetBytes("*3\r\n$3\r\nSET\r\n$");
            _set_header_length = _set_header_buffer.Length;
            _set_command_length = _set_header_buffer.Length + 9;
        }


       
    }

}

using FreeRedis.Client.Protocol.Serialization;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace FreeRedis.Client.Protocol
{
    internal class GetProtocol<T> : IRedisProtocal<T?>
    {
        private static readonly Func<T?> _defaultResultDelegate = () => default;
        private static readonly int? _defaultInt = null;
        private static readonly uint? _defaultUInt = null;
        private static readonly byte? _defaultByte = null;
        private static readonly sbyte? _defaultSByte = null;
        private static readonly short? _defaultShort = null;
        private static readonly ushort? _defaultUShort = null;
        private static readonly long? _defaultLong = null;
        private static readonly ulong? _defaultULong = null;
        private static readonly float? _defaultFloat = null;
        private static readonly double? _defaultDouble = null;
        static GetProtocol()
        {
            var type = typeof(T);
            if (type.BaseType == typeof(Nullable<>))
            {
                if (typeof(T) == typeof(int?))
                {
                    GetProtocol<int?>. _defaultResultDelegate = () => _defaultInt;
                }
                else if (typeof(T) == typeof(uint?))
                {
                    GetProtocol<uint?>._defaultResultDelegate = () => _defaultUInt;
                }
                else if (typeof(T) == typeof(short?))
                {
                    GetProtocol<short?>._defaultResultDelegate = () => _defaultShort;
                }
                else if (typeof(T) == typeof(ushort?))
                {
                    GetProtocol<ushort?>._defaultResultDelegate = () => _defaultUShort;
                }
                else if (typeof(T) == typeof(long?))
                {
                    GetProtocol<long?>._defaultResultDelegate = () => _defaultLong;
                }
                else if (typeof(T) == typeof(ulong?))
                {
                    GetProtocol<ulong?>._defaultResultDelegate = () => _defaultULong;
                }
                else if (typeof(T) == typeof(float?))
                {
                    GetProtocol<float?>._defaultResultDelegate = () => _defaultFloat;
                }
                else if (typeof(T) == typeof(double?))
                {
                    GetProtocol<double?>._defaultResultDelegate = () => _defaultDouble;
                }
                else if (typeof(T) == typeof(byte?))
                {
                    GetProtocol<byte?>._defaultResultDelegate = () => _defaultByte;
                }
                else if (typeof(T) == typeof(sbyte?))
                {
                    GetProtocol<sbyte?>._defaultResultDelegate = () => _defaultSByte;
                }
            }

        }
        public GetProtocol(string key, Action<string>? logger) : base(logger)
        {
            ReadBuffer = Encoding.UTF8.GetBytes($"*2\r\n$3\r\nGET\r\n${key.Length}\r\n{key}\r\n");
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
                T result = ProtocolSerializer<T>.ReadFunc(span[..bufferLength]);
                recvReader.Advance(bufferLength + 2);
                Task.SetResult(result);
                return ProtocolContinueResult.Completed;
            }
            else
            {
                return ProtocolContinueResult.Wait;
            }
        }
        protected override void SetErrorDefaultResult()
        {
            Task.SetResult(_defaultResultDelegate());
        }

    }
}

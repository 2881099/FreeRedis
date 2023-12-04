#if isasync
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RespHelper
    {
        partial class Resp3Reader
        {
            async public Task ReadBlobStringChunkAsync(Stream destination, int bufferSize)
            {
                char c = (char)_stream.ReadByte();
                switch (c)
                {
                    case '$':
                    case '=':
                    case '!': await ReadBlobStringAsync(c, null, destination, bufferSize); break;
                    default: throw new ProtocolViolationException($"Expecting fail MessageType '{c}'");
                }
            }

            async Task<object> ReadBlobStringAsync(char msgtype, Encoding encoding, Stream destination, int bufferSize)
            {
                var clob = await ReadClobAsync();
                if (encoding == null) return clob;
                if (clob == null) return null;
                return encoding.GetString(clob);

                async Task<byte[]> ReadClobAsync()
                {
                    MemoryStream ms = null;
                    try
                    {
                        if (destination == null) destination = ms = new MemoryStream();
                        var lenstr = ReadLine(null);
                        if (int.TryParse(lenstr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var len))
                        {
                            if (len < 0) return null;
                            if (len > 0) await ReadAsync(destination, len, bufferSize);
                            ReadLine(null);
                            if (len == 0) return new byte[0];
                            return ms?.ToArray();
                        }
                        if (lenstr == "?")
                        {
                            while (true)
                            {
                                char c = (char)_stream.ReadByte();
                                if (c != ';') throw new ProtocolViolationException($"Expecting fail Streamed strings ';', got '{c}'");
                                var clenstr = ReadLine(null);
                                if (int.TryParse(clenstr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var clen))
                                {
                                    if (clen == 0) break;
                                    if (clen > 0)
                                    {
                                        await ReadAsync(destination, clen, bufferSize);
                                        ReadLine(null);
                                        continue;
                                    }
                                }
                                throw new ProtocolViolationException($"Expecting fail Streamed strings ';0', got ';{clenstr}'");
                            }
                            return ms?.ToArray();
                        }
                        throw new ProtocolViolationException($"Expecting fail Blob string '{msgtype}0', got '{msgtype}{lenstr}'");
                    }
                    finally
                    {
                        ms?.Close();
                        ms?.Dispose();
                    }
                }
            }

            async Task<object[]> ReadArrayAsync(char msgtype, Encoding encoding)
            {
                var lenstr = ReadLine(null);
                if (int.TryParse(lenstr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var len))
                {
                    if (len < 0) return null;
                    var arr = new object[len];
                    for (var a = 0; a < len; a++)
                        arr[a] = (await ReadObjectAsync(encoding)).Value;
                    if (len == 1 && arr[0] == null) return new object[0];
                    return arr;
                }
                if (lenstr == "?")
                {
                    var arr = new List<object>();
                    while (true)
                    {
                        var ro = await ReadObjectAsync(encoding);
                        if (ro.IsEnd) break;
                        arr.Add(ro.Value);
                    }
                    return arr.ToArray();
                }
                throw new ProtocolViolationException($"Expecting fail Array '{msgtype}3', got '{msgtype}{lenstr}'");
            }
            async Task<object[]> ReadMapAsync(char msgtype, Encoding encoding)
            {
                var lenstr = ReadLine(null);
                if (int.TryParse(lenstr, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var len))
                {
                    if (len < 0) return null;
                    var arr = new object[len * 2];
                    for (var a = 0; a < len; a++)
                    {
                        arr[a * 2] = (await ReadObjectAsync(encoding)).Value;
                        arr[a * 2 + 1] = (await ReadObjectAsync(encoding)).Value;
                    }
                    return arr;
                }
                if (lenstr == "?")
                {
                    var arr = new List<object>();
                    while (true)
                    {
                        var rokey = await ReadObjectAsync(encoding);
                        if (rokey.IsEnd) break;
                        var roval = await ReadObjectAsync(encoding);
                        arr.Add(rokey.Value);
                        arr.Add(roval.Value);
                    }
                    return arr.ToArray();
                }
                throw new ProtocolViolationException($"Expecting fail Map '{msgtype}3', got '{msgtype}{lenstr}'");
            }

            async public Task<RedisResult> ReadObjectAsync(Encoding encoding)
            {
                while (true)
                {
                    var b = _stream.ReadByte();
                    var c = (char)b;
                    //debugger++;
                    //if (debugger > 10000 && debugger % 10 == 0) 
                    //    throw new ProtocolViolationException($"Expecting fail MessageType '{b},{string.Join(",", ReadAll())}'");
                    switch (c)
                    {
                        case '$': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.BlobString);
                        case '+': return new RedisResult(ReadSimpleString(), false, RedisMessageType.SimpleString);
                        case '=': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.VerbatimString);
                        case '-':
                            {
                                var simpleError = ReadSimpleString();
                                if (simpleError == "NOAUTH Authentication required.")
                                    throw new ProtocolViolationException(simpleError);
                                return new RedisResult(simpleError, false, RedisMessageType.SimpleError);
                            }
                        case '!': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.BlobError);
                        case ':': return new RedisResult(ReadNumber(c), false, RedisMessageType.Number);
                        case '(': return new RedisResult(ReadBigNumber(c), false, RedisMessageType.BigNumber);
                        case '_': ReadLine(null); return new RedisResult(null, false, RedisMessageType.Null);
                        case ',': return new RedisResult(ReadDouble(c), false, RedisMessageType.Double);
                        case '#': return new RedisResult(ReadBoolean(c), false, RedisMessageType.Boolean);

                        case '*': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Array);
                        case '~': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Set);
                        case '>': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Push);
                        case '%': return new RedisResult(await ReadMapAsync(c, encoding), false, RedisMessageType.Map);
                        case '|': return new RedisResult(await ReadMapAsync(c, encoding), false, RedisMessageType.Attribute);
                        case '.': ReadLine(null); return new RedisResult(null, true, RedisMessageType.SimpleString); //无类型
                        case ' ': continue;
                        default:
                            if (b == -1) return new RedisResult(null, true, RedisMessageType.Null);
                            //if (b == -1) return new RedisResult(null, true, RedisMessageType.Null);
                            var allBytes = DebugReadAll();
                            throw new ProtocolViolationException($"Expecting fail MessageType '{b},{string.Join(",", allBytes)}'");
                    }
                }
            }

            async Task ReadAsync(Stream outStream, int len, int bufferSize = 1024)
            {
                if (len <= 0) return;
                var buffer = new byte[Math.Min(bufferSize, len)];
                var bufferLength = buffer.Length;
                while (true)
                {
                    var readed = await _stream.ReadAsync(buffer, 0, bufferLength);
                    if (readed <= 0) throw new ProtocolViolationException($"Expecting fail Read surplus length: {len}");
                    if (readed > 0) await outStream.WriteAsync(buffer, 0, readed);
                    len = len - readed;
                    if (len <= 0) break;
                    if (len < buffer.Length) bufferLength = len;
                }
            }
        }
    }
}
#endif
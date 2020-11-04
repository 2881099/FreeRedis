#if isasync
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RespHelper
    {
        partial class Resp3Reader
        {
            //Stream _stream; //test code

            async Task<byte> ReadByteAsync()
            {
                var bytes = new byte[1];
                var readed = await _stream.ReadAsync(bytes, 0, 1);
                if (readed <= 0) throw new ProtocolViolationException($"Expecting fail ReadByte end of stream");
                return bytes.FirstOrDefault();
            }
            async Task<char> ReadCharAsync() => (char)(await ReadByteAsync());

            async public Task ReadBlobStringChunkAsync(Stream destination, int bufferSize)
            {
                char c = await ReadCharAsync();
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
                var clob = await ReadClob();
                if (encoding == null) return clob;
                if (clob == null) return null;
                return encoding.GetString(clob);

                async Task<byte[]> ReadClob()
                {
                    MemoryStream ms = null;
                    try
                    {
                        if (destination == null) destination = ms = new MemoryStream();
                        var lenstr = await ReadLineAsync(null);
                        if (int.TryParse(lenstr, out var len))
                        {
                            if (len < 0) return null;
                            if (len > 0) await ReadAsync(destination, len, bufferSize);
                            await ReadLineAsync(null);
                            if (len == 0) return new byte[0];
                            return ms?.ToArray();
                        }
                        if (lenstr == "?")
                        {
                            while (true)
                            {
                                char c = await ReadCharAsync();
                                if (c != ';') throw new ProtocolViolationException($"Expecting fail Streamed strings ';', got '{c}'");
                                var clenstr = await ReadLineAsync(null);
                                if (int.TryParse(clenstr, out var clen))
                                {
                                    if (clen == 0) break;
                                    if (clen > 0)
                                    {
                                        await ReadAsync(destination, clen, bufferSize);
                                        await ReadLineAsync(null);
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
            Task<string> ReadSimpleStringAsync()
            {
                return ReadLineAsync(null);
            }
            async Task<long> ReadNumberAsync(char msgtype)
            {
                var numstr = await ReadLineAsync(null);
                if (long.TryParse(numstr, out var num)) return num;
                throw new ProtocolViolationException($"Expecting fail Number '{msgtype}0', got '{msgtype}{numstr}'");
            }
            async Task<BigInteger> ReadBigNumberAsync(char msgtype)
            {
                var numstr = await ReadLineAsync(null);
                if (BigInteger.TryParse(numstr, NumberStyles.Any, null, out var num)) return num;
                throw new ProtocolViolationException($"Expecting fail BigNumber '{msgtype}0', got '{msgtype}{numstr}'");
            }
            async Task<double> ReadDoubleAsync(char msgtype)
            {
                var numstr = await ReadLineAsync(null);
                switch (numstr)
                {
                    case "inf": return double.PositiveInfinity;
                    case "-inf": return double.NegativeInfinity;
                }
                if (double.TryParse(numstr, NumberStyles.Any, null, out var num)) return num;
                throw new ProtocolViolationException($"Expecting fail Double '{msgtype}1.23', got '{msgtype}{numstr}'");
            }
            async Task<bool> ReadBooleanAsync(char msgtype)
            {
                var boolstr = await ReadLineAsync(null);
                switch (boolstr)
                {
                    case "t": return true;
                    case "f": return false;
                }
                throw new ProtocolViolationException($"Expecting fail Boolean '{msgtype}t', got '{msgtype}{boolstr}'");
            }

            async Task<object[]> ReadArrayAsync(char msgtype, Encoding encoding)
            {
                var lenstr = await ReadLineAsync(null);
                if (int.TryParse(lenstr, out var len))
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
                var lenstr = await ReadLineAsync(null);
                if (int.TryParse(lenstr, out var len))
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
                    var c = await ReadCharAsync();
                    switch (c)
                    {
                        case '$': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.BlobString);
                        case '+': return new RedisResult(await ReadSimpleStringAsync(), false, RedisMessageType.SimpleString);
                        case '=': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.VerbatimString);
                        case '-': return new RedisResult(await ReadSimpleStringAsync(), false, RedisMessageType.SimpleError);
                        case '!': return new RedisResult(await ReadBlobStringAsync(c, encoding, null, 1024), false, RedisMessageType.BlobError);
                        case ':': return new RedisResult(await ReadNumberAsync(c), false, RedisMessageType.Number);
                        case '(': return new RedisResult(await ReadBigNumberAsync(c), false, RedisMessageType.BigNumber);
                        case '_': await ReadLineAsync(null); return new RedisResult(null, false, RedisMessageType.Null);
                        case ',': return new RedisResult(await ReadDoubleAsync(c), false, RedisMessageType.Double);
                        case '#': return new RedisResult(await ReadBooleanAsync(c), false, RedisMessageType.Boolean);

                        case '*': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Array);
                        case '~': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Set);
                        case '>': return new RedisResult(await ReadArrayAsync(c, encoding), false, RedisMessageType.Push);
                        case '%': return new RedisResult(await ReadMapAsync(c, encoding), false, RedisMessageType.Map);
                        case '|': return new RedisResult(await ReadMapAsync(c, encoding), false, RedisMessageType.Attribute);
                        case '.': await ReadLineAsync(null); return new RedisResult(null, true, RedisMessageType.SimpleString); //无类型
                        case ' ': continue;
                        default: throw new ProtocolViolationException($"Expecting fail MessageType '{c}'");
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
            async Task<string> ReadLineAsync(Stream outStream)
            {
                var sb = outStream == null ? new StringBuilder() : null;
                var buffer = new byte[1];
                var should_break = false;
                while (true)
                {
                    var readed = await _stream.ReadAsync(buffer, 0, 1);
                    if (readed <= 0) throw new ProtocolViolationException($"Expecting fail ReadLine end of stream");
                    if (buffer[0] == 13)
                        should_break = true;
                    else if (buffer[0] == 10 && should_break)
                        break;
                    else
                    {
                        if (outStream == null) sb.Append((char)buffer[0]);
                        else await outStream.WriteAsync(buffer, 0, 1);
                        should_break = false;
                    }
                }
                return sb?.ToString();
            }
        }
    }
}
#endif
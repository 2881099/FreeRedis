using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public interface IRedisSocket : IDisposable
    {
        string Host { get; }
        bool Ssl { get; }
        Socket Socket { get; }
        Stream Stream { get; }
        bool IsConnected { get; }
        event EventHandler<EventArgs> Connected;

        RedisClient Client { get; }

        RedisProtocol Protocol { get; set; }
        Encoding Encoding { get; set; }

        void Write(CommandBuilder cmd);
        void Write(Encoding encoding, CommandBuilder cmd);

        RedisResult<T> Read<T>();
        RedisResult<T> Read<T>(Encoding encoding);
        void ReadChunk(Stream destination, int bufferSize = 1024);

        void Connect(int millisecondsTimeout = 15000);
#if net40
#else
        Task ConnectAsync(int millisecondsTimeout = 15000);
#endif

        void ResetHost(string host);
    }
}

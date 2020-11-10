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
        TimeSpan ConnectTimeout { get; set; }
        TimeSpan ReceiveTimeout { get; set; }
        TimeSpan SendTimeout { get; set; }

        Socket Socket { get; }
        Stream Stream { get; }
        bool IsConnected { get; }
        event EventHandler<EventArgs> Connected;

        RedisProtocol Protocol { get; set; }
        Encoding Encoding { get; set; }

        void Write(CommandPacket cmd);
        RedisResult Read(CommandPacket cmd);
        void ReadChunk(Stream destination, int bufferSize = 1024);
        ClientReplyType ClientReply { get; }
        long ClientId { get; }
        int Database { get; }

        void Connect();

        void ResetHost(string host);
        void ReleaseSocket();
    }
}

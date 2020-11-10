using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
    public interface IRedisSocketModify
    {
        void SetClientReply(ClientReplyType value);
        void SetClientId(long value);
        void SetDatabase(int value);
    }

    class DefaultRedisSocket : IRedisSocket, IRedisSocketModify
    {
        internal static TempProxyRedisSocket CreateTempProxy(IRedisSocket rds, Action dispose)
        {
            if (rds is TempProxyRedisSocket proxy) 
                return new TempProxyRedisSocket(proxy._owner, dispose);
            return new TempProxyRedisSocket(rds, dispose);
        }
        internal class TempProxyRedisSocket : IRedisSocket, IRedisSocketModify
        {
            internal string _poolkey; //flag idlebus key
            internal RedisClientPool _pool; //flag pooling
            internal IRedisSocket _owner;
            Action _dispose;
            public TempProxyRedisSocket(IRedisSocket owner, Action dispose)
            {
                _owner = owner;
                _dispose = dispose;
            }

            public void Dispose() => _dispose?.Invoke();

            public string Host => _owner.Host;
            public bool Ssl => _owner.Ssl;
            public TimeSpan ConnectTimeout { get => _owner.ConnectTimeout; set => _owner.ConnectTimeout = value; }
            public TimeSpan ReceiveTimeout { get => _owner.ReceiveTimeout; set => _owner.ReceiveTimeout = value; }
            public TimeSpan SendTimeout { get => _owner.SendTimeout; set => _owner.SendTimeout = value; }
            public Socket Socket => _owner.Socket;
            public Stream Stream => _owner.Stream;
            public bool IsConnected => _owner.IsConnected;
            public RedisProtocol Protocol { get => _owner.Protocol; set => _owner.Protocol = value; }
            public Encoding Encoding { get => _owner.Encoding; set => _owner.Encoding = value; }
            public event EventHandler<EventArgs> Connected { add { _owner.Connected += value; } remove { _owner.Connected -= value; } }
            public ClientReplyType ClientReply => _owner.ClientReply;
            public long ClientId => _owner.ClientId;
            public int Database => _owner.Database;

            void IRedisSocketModify.SetClientReply(ClientReplyType value) => (_owner as IRedisSocketModify).SetClientReply(value);
            void IRedisSocketModify.SetClientId(long value) => (_owner as IRedisSocketModify).SetClientId(value);
            void IRedisSocketModify.SetDatabase(int value) => (_owner as IRedisSocketModify).SetDatabase(value);

            public void Connect() => _owner.Connect();
            public void ResetHost(string host) => _owner.ResetHost(host);
            public void ReleaseSocket() => _owner.ReleaseSocket();
            public void Write(CommandPacket cmd) => _owner.Write(cmd);
            public RedisResult Read(CommandPacket cmd) => _owner.Read(cmd);
            public void ReadChunk(Stream destination, int bufferSize = 1024) => _owner.ReadChunk(destination, bufferSize);
        }

        public string Host { get; private set; }
        public bool Ssl { get; private set; }
        string _ip;
        int _port;
        TimeSpan _receiveTimeout = TimeSpan.FromSeconds(20);
        TimeSpan _sendTimeout = TimeSpan.FromSeconds(20);
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                if (_socket != null) _socket.ReceiveTimeout = (int)value.TotalMilliseconds;
                _receiveTimeout = value;
            }
        }
        public TimeSpan SendTimeout
        {
            get => _sendTimeout;
            set
            {
                if (_socket != null) _socket.SendTimeout = (int)value.TotalMilliseconds;
                _sendTimeout = value;
            }
        }

        Socket _socket;
        public Socket Socket => _socket ?? throw new RedisClientException("Redis socket connection was not opened");
        NetworkStream _stream;
        public Stream Stream => _stream ?? throw new RedisClientException("Redis socket connection was not opened");
        public bool IsConnected => _socket?.Connected == true && _stream != null;
        public event EventHandler<EventArgs> Connected;
        public ClientReplyType ClientReply { get; protected set; } = ClientReplyType.on;
        public long ClientId { get; protected set; }
        public int Database { get; protected set; } = 0;

        void IRedisSocketModify.SetClientReply(ClientReplyType value) => this.ClientReply = value;
        void IRedisSocketModify.SetClientId(long value) => this.ClientId = value;
        void IRedisSocketModify.SetDatabase(int value) => this.Database = value;

        RespHelper.Resp3Reader _reader;
        RespHelper.Resp3Reader Reader => _reader ?? (_reader = new RespHelper.Resp3Reader(Stream));

        public RedisProtocol Protocol { get; set; } = RedisProtocol.RESP2;
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public DefaultRedisSocket(string host, bool ssl)
        {
            Host = host;
            Ssl = ssl;
        }

        public void Write(CommandPacket cmd)
        {
            if (IsConnected == false) Connect();
            using (var ms = new MemoryStream()) //Writing data directly to will be very slow
            {
                new RespHelper.Resp3Writer(ms, Encoding, Protocol).WriteCommand(cmd);
                ms.Position = 0;
                ms.CopyTo(Stream);
                ms.Close();
            }
            switch (cmd._command)
            {
                case "CLIENT":
                    switch (cmd._subcommand)
                    {
                        case "REPLY":
                            var type = cmd._input.LastOrDefault().ConvertTo<ClientReplyType>();
                            if (type != ClientReply) ClientReply = type;
                            break;
                        case "ID":
                            cmd.OnData(rt =>
                            {
                                ClientId = rt.ThrowOrValue<long>();
                            });
                            break;
                    }
                    break;
                case "SELECT":
                    var dbidx = cmd._input.LastOrDefault()?.ConvertTo<int?>();
                    if (dbidx != null) Database = dbidx.Value;
                    break;
            }
            cmd.WriteHost = this.Host;
        }
        public RedisResult Read(CommandPacket cmd)
        {
            if (ClientReply == ClientReplyType.on)
            {
                if (IsConnected == false) Connect();
                var rt = Reader.ReadObject(cmd?._flagReadbytes == true ? null : Encoding);
                rt.Encoding = Encoding;
                cmd?.OnDataTrigger(rt);
                return rt;
            }
            return new RedisResult(null, true, RedisMessageType.SimpleString) { Encoding = Encoding };
        }
        public void ReadChunk(Stream destination, int bufferSize = 1024)
        {
            if (ClientReply == ClientReplyType.on)
            {
                if (IsConnected == false) Connect();
                Reader.ReadBlobStringChunk(destination, bufferSize);
            }
        }

        public void Connect()
        {
            ResetHost(Host);
            var endpoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
            var localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var asyncResult = localSocket.BeginConnect(endpoint, null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout, true))
                throw new TimeoutException("Connect to redis-server timeout");
            _socket = localSocket;
            _stream = new NetworkStream(Socket, true);
            _socket.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
            _socket.SendTimeout = (int)SendTimeout.TotalMilliseconds;
            Connected?.Invoke(this, new EventArgs());
        }

        public void ResetHost(string host)
        {
            this.Host = host;
            ReleaseSocket();
            var sh = SplitHost(host);
            _ip = sh.Key;
            _port = sh.Value;
        }

        public void ReleaseSocket()
        {
            if (_socket != null)
            {
                try { _socket.Shutdown(SocketShutdown.Both); } catch { }
                try { _socket.Close(); } catch { }
                try { _socket.Dispose(); } catch { }
                _socket = null;
            }
            if (_stream != null)
            {
                try { _stream.Close(); } catch { }
                try { _stream.Dispose(); } catch { }
                _stream = null;
            }
            _reader = null;
        }

        public void Dispose()
        {
            ReleaseSocket();
        }

        public static KeyValuePair<string, int> SplitHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host?.Trim()))
                return new KeyValuePair<string, int>("127.0.0.1", 6379);

            host = host.Trim();
            var ipv6 = Regex.Match(host, @"^\[([^\]]+)\]\s*(:\s*(\d+))?$");
            if (ipv6.Success) //ipv6+port 格式： [fe80::b164:55b3:4b4f:7ce6%15]:6379
                return new KeyValuePair<string, int>(ipv6.Groups[1].Value.Trim(), 
                    int.TryParse(ipv6.Groups[3].Value, out var tryint) && tryint > 0 ? tryint : 6379);

            var spt = (host ?? "").Split(':');
            if (spt.Length == 1) //ipv4 or domain
                return new KeyValuePair<string, int>(string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1", 6379);

            if (spt.Length == 2) //ipv4:port or domain:port
            {
                if (int.TryParse(spt.Last().Trim(), out var testPort2))
                    return new KeyValuePair<string, int>(string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1", testPort2);

                return new KeyValuePair<string, int>(host, 6379);
            }

            if (IPAddress.TryParse(host, out var tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6) //test ipv6
                return new KeyValuePair<string, int>(host, 6379);

            if (int.TryParse(spt.Last().Trim(), out var testPort)) //test ipv6:port
            {
                var testHost = string.Join(":", spt.Where((a, b) => b < spt.Length - 1));
                if (IPAddress.TryParse(testHost, out tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6)
                    return new KeyValuePair<string, int>(testHost, 6379);
            }

            return new KeyValuePair<string, int>(host, 6379);
        }
    }
}

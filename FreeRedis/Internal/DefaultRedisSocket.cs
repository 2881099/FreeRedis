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
    class DefaultRedisSocket : IRedisSocket
    {
        public static IRedisSocket CreateTempProxy(IRedisSocket rds, Action dispose)
        {
            if (rds is TempProxyRedisSocket proxy) 
                return new TempProxyRedisSocket(proxy._owner, dispose);
            return new TempProxyRedisSocket(rds, dispose);
        }
        class TempProxyRedisSocket : IRedisSocket
        {
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

            public void Connect() => _owner.Connect();
#if net40
#else
            public Task ConnectAsync() => _owner.ConnectAsync();
#endif
            public void ResetHost(string host) => _owner.ResetHost(host);
            public void Write(CommandPacket cmd) => _owner.Write(cmd);
            public ClientReplyType ClientReply => _owner.ClientReply;
        }

        public string Host { get; private set; }
        public bool Ssl { get; private set; }
        string _ip;
        int _port;
        TimeSpan _receiveTimeout = TimeSpan.FromSeconds(10);
        TimeSpan _sendTimeout = TimeSpan.FromSeconds(10);
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
        public Socket Socket => _socket ?? throw new Exception("Redis socket connection was not opened");
        NetworkStream _stream;
        public Stream Stream => _stream ?? throw new Exception("Redis socket connection was not opened");
        public bool IsConnected => _socket?.Connected == true && _stream != null;
        public event EventHandler<EventArgs> Connected;

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
            RespHelper.Write(Stream, Encoding, cmd, Protocol);
            if (string.Compare(cmd._command, "CLIENT", true) == 0 &&
                string.Compare(cmd._subcommand, "REPLY", true) == 0)
            {
                var type = cmd._input.FirstOrDefault().ConvertTo<ClientReplyType>();
                if (type != ClientReply) ClientReply = type;
            }
            cmd._redisSocket = this;
        }
        public ClientReplyType ClientReply { get; protected set; }

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
#if net40
#else
        TaskCompletionSource<bool> connectAsyncTcs;
        async public Task ConnectAsync()
        {
            ResetHost(Host);
            var endpoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
            var localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            connectAsyncTcs?.TrySetCanceled();
            connectAsyncTcs = new TaskCompletionSource<bool>();
            localSocket.BeginConnect(endpoint, asyncResult =>
            {
                try
                {
                    localSocket.EndConnect(asyncResult);
                    connectAsyncTcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    connectAsyncTcs.TrySetException(ex);
                }
            }, null);
            await connectAsyncTcs.Task.TimeoutAfter(ConnectTimeout, "Connect to redis-server timeout");
            _socket = localSocket;
            _stream = new NetworkStream(Socket, true);
            _socket.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
            _socket.SendTimeout = (int)SendTimeout.TotalMilliseconds;
            Connected?.Invoke(this, new EventArgs());
        }
#endif

        public void ResetHost(string host)
        {
            SafeReleaseSocket();
            if (string.IsNullOrWhiteSpace(host?.Trim()))
            {
                _ip = "127.0.0.1";
                _port = 6379;
                return;
            }
            host = host.Trim();
            var ipv6 = Regex.Match(host, @"^\[([^\]]+)\]\s*(:\s*(\d+))?$");
            if (ipv6.Success) //ipv6+port 格式： [fe80::b164:55b3:4b4f:7ce6%15]:6379
            {
                _ip = ipv6.Groups[1].Value.Trim();
                _port = int.TryParse(ipv6.Groups[3].Value, out var tryint) && tryint > 0 ? tryint : 6379;
                return;
            }
            var spt = (host ?? "").Split(':');
            if (spt.Length == 1) //ipv4 or domain
            {
                _ip = string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
                _port = 6379;
                return;
            }
            if (spt.Length == 2) //ipv4:port or domain:port
            {
                if (int.TryParse(spt.Last().Trim(), out var testPort2))
                {
                    _ip = string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
                    _port = testPort2;
                    return;
                }
                _ip = host;
                _port = 6379;
                return;
            }
            if (IPAddress.TryParse(host, out var tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6) //test ipv6
            {
                _ip = host;
                _port = 6379;
                return;
            }
            if (int.TryParse(spt.Last().Trim(), out var testPort)) //test ipv6:port
            {
                var testHost = string.Join(":", spt.Where((a, b) => b < spt.Length - 1));
                if (IPAddress.TryParse(testHost, out tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _ip = testHost;
                    _port = 6379;
                    return;
                }
            }
            _ip = host;
            _port = 6379;
        }

        public void SafeReleaseSocket()
        {
            if (_stream != null)
            {
                try { _stream.Close(); } catch { }
                try { _stream.Dispose(); } catch { }
                _stream = null;
            }
            if (_socket != null)
            {
                try { _socket.Shutdown(SocketShutdown.Both); } catch { }
                try { _socket.Close(); } catch { }
                try { _socket.Dispose(); } catch { }
                _socket = null;
            }
        }

        ~DefaultRedisSocket() => this.Dispose();
        int _disposeCounter;
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCounter) != 1) return;
            try
            {
                SafeReleaseSocket();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}

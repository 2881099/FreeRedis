﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public interface IRedisSocketModify
    {
        void SetClientReply(ClientReplyType value);
        void SetClientId(long value);
        void SetDatabase(int value);
    }

    public class DefaultRedisSocket : IRedisSocket, IRedisSocketModify
    {
        public static TempProxyRedisSocket CreateTempProxy(IRedisSocket rds, Action dispose)
        {
            if (rds is TempProxyRedisSocket proxy) 
                return new TempProxyRedisSocket(proxy._owner, dispose);
            return new TempProxyRedisSocket(rds, dispose);
        }
        public class TempProxyRedisSocket : IRedisSocket, IRedisSocketModify
        {
            public string _poolkey; //flag idlebus key
            public RedisClientPool _pool; //flag pooling
            public IRedisSocket _owner;
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
            public event EventHandler<EventArgs> Disconnected { add { _owner.Disconnected += value; } remove { _owner.Disconnected -= value; } }
            public ClientReplyType ClientReply => _owner.ClientReply;
            public long ClientId => _owner.ClientId;
            public Guid ClientId2 => _owner.ClientId2;
            public int Database => _owner.Database;
            public CommandPacket LastCommand => _owner.LastCommand;

            void IRedisSocketModify.SetClientReply(ClientReplyType value) => (_owner as IRedisSocketModify).SetClientReply(value);
            void IRedisSocketModify.SetClientId(long value) => (_owner as IRedisSocketModify).SetClientId(value);
            void IRedisSocketModify.SetDatabase(int value) => (_owner as IRedisSocketModify).SetDatabase(value);

            public void Connect() => _owner.Connect();
            public void ResetHost(string host) => _owner.ResetHost(host);
            public void ReleaseSocket() => _owner.ReleaseSocket();
            public void Write(CommandPacket cmd) => _owner.Write(cmd);
            public RedisResult Read(CommandPacket cmd) => _owner.Read(cmd);
            public void ReadChunk(Stream destination, int bufferSize = 1024) => _owner.ReadChunk(destination, bufferSize);
#if isasync
            public Task WriteAsync(CommandPacket cmd) => _owner.WriteAsync(cmd);
            public Task<RedisResult> ReadAsync(CommandPacket cmd) => _owner.ReadAsync(cmd);
            public Task ReadChunkAsync(Stream destination, int bufferSize = 1024) => _owner.ReadChunkAsync(destination, bufferSize);
#endif
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
        NetworkStream _netStream;
        SslStream _sslStream;
        public Stream Stream => ((Stream)_sslStream ?? _netStream) ?? throw new RedisClientException("Redis socket connection was not opened");
        public bool IsConnected => _socket?.Connected == true && _netStream != null;
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;
        public ClientReplyType ClientReply { get; protected set; } = ClientReplyType.on;
        public long ClientId { get; protected set; }
        public Guid ClientId2 { get; } = Guid.NewGuid();
        public int Database { get; protected set; } = 0;
        public CommandPacket LastCommand { get; protected set; }

        void IRedisSocketModify.SetClientReply(ClientReplyType value) => this.ClientReply = value;
        void IRedisSocketModify.SetClientId(long value) => this.ClientId = value;
        void IRedisSocketModify.SetDatabase(int value) => this.Database = value;

        RespHelper.Resp3Reader _reader;
        RespHelper.Resp3Reader Reader => _reader ?? (_reader = new RespHelper.Resp3Reader(Stream));

        public RedisProtocol Protocol { get; set; } = RedisProtocol.RESP2;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        RemoteCertificateValidationCallback _certificateValidation;
        LocalCertificateSelectionCallback _certificateSelection;

        public DefaultRedisSocket(string host, bool ssl, RemoteCertificateValidationCallback certificateValidation, LocalCertificateSelectionCallback certificateSelection)
        {
            Host = host;
            Ssl = ssl;
            _certificateValidation = certificateValidation;
            _certificateSelection = certificateSelection;
        }

        void WriteAfter(CommandPacket cmd)
        {
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
            cmd.WriteTarget = $"{this.Host}/{this.Database}";
            cmd.ClientId2 = ClientId2;
        }
        public void Write(CommandPacket cmd)
        {
            LastCommand = cmd;
            if (IsConnected == false) Connect();
            using (var ms = new MemoryStream()) //Writing data directly to will be very slow
            {
                new RespHelper.Resp3Writer(ms, Encoding, Protocol).WriteCommand(cmd);
                ms.Position = 0;
                ms.CopyTo(Stream);
                ms.Close();
            }
            WriteAfter(cmd);
        }
        public RedisResult Read(CommandPacket cmd)
        {
            LastCommand = cmd;
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
#if isasync
        async public Task WriteAsync(CommandPacket cmd)
        {
            LastCommand = cmd;
            if (IsConnected == false) Connect();
            using (var ms = new MemoryStream()) //Writing data directly to will be very slow
            {
                new RespHelper.Resp3Writer(ms, Encoding, Protocol).WriteCommand(cmd);
                ms.Position = 0;
                await ms.CopyToAsync(Stream);
                ms.Close();
            }
            WriteAfter(cmd);
        }
        async public Task<RedisResult> ReadAsync(CommandPacket cmd)
        {
            LastCommand = cmd;
            if (ClientReply == ClientReplyType.on)
            {
                if (IsConnected == false) Connect();
                var rt = await Reader.ReadObjectAsync(cmd?._flagReadbytes == true ? null : Encoding);
                rt.Encoding = Encoding;
                cmd?.OnDataTrigger(rt);
                return rt;
            }
            return new RedisResult(null, true, RedisMessageType.SimpleString) { Encoding = Encoding };
        }
        async public Task ReadChunkAsync(Stream destination, int bufferSize = 1024)
        {
            if (ClientReply == ClientReplyType.on)
            {
                if (IsConnected == false) Connect();
                await Reader.ReadBlobStringChunkAsync(destination, bufferSize);
            }
        }
#endif

        object _connectLock = new object();
        public void Connect()
        {
            lock (_connectLock)
            {
                ResetHost(Host);

                EndPoint endpoint = IPAddress.TryParse(_ip, out var tryip) ?
                    (EndPoint)new IPEndPoint(tryip, _port) :
                    new DnsEndPoint(_ip, _port);

                var localSocket = endpoint.AddressFamily == AddressFamily.InterNetworkV6 ? 
                    new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp):
                    new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    var asyncResult = localSocket.BeginConnect(endpoint, null, null);
                    if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout, true))
                    {
                        var endpointString = endpoint.ToString();
                        if (endpointString != $"{_ip}:{_port}") endpointString = $"{_ip}:{_port} -> {endpointString}";
                        var debugString = "";
                        if (endpoint is DnsEndPoint)
                        {
                            try { debugString = $", DEBUG: Dns.GetHostEntry({_ip})={Dns.GetHostEntry(_ip)}"; }
                            catch (Exception ex) { debugString = $", DEBUG: {ex.Message}"; }
                        }
                        throw new TimeoutException($"Connect to redis-server({endpointString}) timeout{debugString}");
                    }
                    localSocket.EndConnect(asyncResult);
                }
                catch
                {
                    ReleaseSocket(localSocket);
                    throw;
                }
                _socket = localSocket;
                _socket.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
                _socket.SendTimeout = (int)SendTimeout.TotalMilliseconds;
                _netStream = new NetworkStream(Socket, true);
                if (Ssl)
                {
                    _sslStream = new SslStream(_netStream, true, _certificateValidation, _certificateSelection);
                    var stringHostOnly = endpoint is DnsEndPoint ep1 ? ep1.Host :
                        (endpoint is IPEndPoint ep2 ? ep2.Address.ToString() : "");
                    _sslStream.AuthenticateAsClient(stringHostOnly);
                }
                Connected?.Invoke(this, new EventArgs());
            }
        }

        public void ResetHost(string host)
        {
            this.Host = host;
            ReleaseSocket();
            var sh = SplitHost(host);
            _ip = sh.Key;
            _port = sh.Value;
        }

        void ReleaseSocket(Socket socket)
        {
            if (socket == null) return;
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            try { socket.Close(); } catch { }
            try { socket.Dispose(); } catch { }
        }
        public void ReleaseSocket()
        {
            lock (_connectLock)
            {
                if (_socket != null)
                {
                    ReleaseSocket(_socket);
                    _socket = null;
                }
                if (_sslStream != null)
                {
                    try { _sslStream.Close(); } catch { }
                    try { _sslStream.Dispose(); } catch { }
                    _sslStream = null;
                }
                if (_netStream != null)
                {
                    try { _netStream.Close(); } catch { }
                    try { _netStream.Dispose(); } catch { }
                    _netStream = null;
                }
                _reader = null;
                Disconnected?.Invoke(this, new EventArgs());
            }
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

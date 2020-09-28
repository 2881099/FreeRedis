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

namespace FreeRedis
{
    public partial class RedisClient : IDisposable
    {
        string _initHost;
        public bool Ssl { get; private set; }
        public string Ip { get; private set; }
        public int Port { get; private set; }

        Socket _socket;
        public Socket Socket => _socket ?? throw new Exception("Redis connection was not opened");
        NetworkStream _stream;
        public Stream Stream => _stream ?? throw new Exception("Redis connection was not opened");
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public bool IsConnected => Socket != null && Socket.Connected && Stream != null ? true : false;

        public RedisClient(string host, bool ssl)
		{
            _initHost = host;
            Ssl = ssl;
		}

        string _listeningCommand;
        object[] PrepareCommand(string command, string subcommand = null, params object[] parms)
        {
            if (!string.IsNullOrEmpty(_listeningCommand)) throw new Exception($"无法进行新的操作，因为正在执行监听的命令：{_listeningCommand}");
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentNullException("Redis command not is null or empty.");
            object[] args = null;
            if (parms?.Any() != true)
            {
                if (string.IsNullOrWhiteSpace(subcommand) == false) args = new object[] { command, subcommand };
                else args = command.Split(' ').Where(a => string.IsNullOrWhiteSpace(a) == false).ToArray();
            }
            else
            {
                var issubcmd = string.IsNullOrWhiteSpace(subcommand) == false;
                args = new object[parms.Length + 1 + (issubcmd ? 1 : 0)];
                var argsidx = 0;
                args[argsidx++] = command;
                if (issubcmd) args[argsidx++] = subcommand;
                foreach (var prm in parms) args[argsidx++] = prm;
            }
            return args;
        }

        #region Connect
        public void Connect(int millisecondsTimeout)
        {
            ResetHost(_initHost);
            var endpoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
            var localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var asyncResult = localSocket.BeginConnect(endpoint, null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne(millisecondsTimeout, true))
                throw new TimeoutException("Connect to redis-server timeout");
            _socket = localSocket;
            _stream = new NetworkStream(Socket, true);
        }
        TaskCompletionSource<bool> connectAsyncTcs;
        async public Task ConnectAsync(int millisecondsTimeout)
        {
            ResetHost(_initHost);
            var endpoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
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
            await connectAsyncTcs.Task.TimeoutAfter(TimeSpan.FromMilliseconds(millisecondsTimeout), "Connect to redis-server timeout");
            _socket = localSocket;
            _stream = new NetworkStream(Socket, true);
        }
        #endregion

        public void Dispose()
        {
            ReleaseSocket();
        }
        void ReleaseSocket()
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
		void ResetHost(string host)
        {
            ReleaseSocket();
            if (string.IsNullOrEmpty(host?.Trim()))
            {
                Ip = "127.0.0.1";
                Port = 6379;
                return;
            }
            host = host.Trim();
            var ipv6 = Regex.Match(host, @"^\[([^\]]+)\]\s*(:\s*(\d+))?$");
            if (ipv6.Success) //ipv6+port 格式： [fe80::b164:55b3:4b4f:7ce6%15]:6379
            {
                Ip = ipv6.Groups[1].Value.Trim();
                Port = int.TryParse(ipv6.Groups[3].Value, out var tryint) && tryint > 0 ? tryint : 6379;
                return;
            }
            var spt = (host ?? "").Split(':');
            if (spt.Length == 1) //ipv4 or domain
            {
                Ip = string.IsNullOrEmpty(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
                Port = 6379;
                return;
            }
            if (spt.Length == 2) //ipv4:port or domain:port
            {
                if (int.TryParse(spt.Last().Trim(), out var testPort2))
                {
                    Ip = string.IsNullOrEmpty(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
                    Port = testPort2;
                    return;
                }
                Ip = host;
                Port = 6379;
                return;
            }
            if (IPAddress.TryParse(host, out var tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6) //test ipv6
            {
                Ip = host;
                Port = 6379;
                return;
            }
            if (int.TryParse(spt.Last().Trim(), out var testPort)) //test ipv6:port
            {
                var testHost = string.Join(":", spt.Where((a, b) => b < spt.Length - 1));
                if (IPAddress.TryParse(testHost, out tryip) && tryip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    Ip = testHost;
                    Port = 6379;
                    return;
                }
            }
            Ip = host;
            Port = 6379;
        }
	}
}

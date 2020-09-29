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
    //Pipelining: Learn how to send multiple commands at once, saving on round trip time.
    //Redis transactions: It is possible to group commands together so that they are executed as a single transaction.
    //Client side caching: Starting with version 6 Redis supports server assisted client side caching. This document describes how to use it.
    //Distributed locks: Implementing a distributed lock manager with Redis.
    //Redis keyspace notifications: Get notifications of keyspace events via Pub/Sub (Redis 2.8 or greater).
    //High Availability: Redis Sentinel is the official high availability solution for Redis.

    public abstract class RedisClientBase
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

        public RedisClientBase(string host, bool ssl)
		{
            _initHost = host;
            Ssl = ssl;
		}

        string _CallReadWhileCmd;
        object[] PrepareCmd(string cmd, string subcmd = null, params object[] parms)
        {
            if (!string.IsNullOrWhiteSpace(_CallReadWhileCmd)) throw new Exception($"无法进行新的操作，因为正在执行监听的命令：{_CallReadWhileCmd}");
            if (string.IsNullOrWhiteSpace(cmd)) throw new ArgumentNullException("Redis command not is null or empty.");
            object[] args = null;
            if (parms?.Any() != true)
            {
                if (string.IsNullOrWhiteSpace(subcmd) == false) args = new object[] { cmd, subcmd };
                else args = cmd.Split(' ').Where(a => string.IsNullOrWhiteSpace(a) == false).ToArray();
            }
            else
            {
                var issubcmd = string.IsNullOrWhiteSpace(subcmd) == false;
                args = new object[parms.Length + 1 + (issubcmd ? 1 : 0)];
                var argsidx = 0;
                args[argsidx++] = cmd;
                if (issubcmd) args[argsidx++] = subcmd;
                foreach (var prm in parms) args[argsidx++] = prm;
            }
            return args;
        }

        protected RedisResult<T> Call<T>(string cmd, string subcmd = null, params object[] parms)
        {
            var args = PrepareCmd(cmd, subcmd, parms);
            Resp3Helper.Write(Stream, Encoding, args, true);
            var result = Resp3Helper.Read<T>(Stream, Encoding);
            return result;
        }
        protected void CallWriteOnly(string cmd, string subcmd = null, params object[] parms)
        {
            var args = PrepareCmd(cmd, subcmd, parms);
            Resp3Helper.Write(Stream, Encoding, args, true);
        }
        protected void CallReadWhile(Action<object> ondata, Func<bool> next, string command, string subcommand = null, params object[] parms)
        {
            var args = PrepareCmd(command, subcommand, parms);
            Resp3Helper.Write(Stream, args, true);
            _CallReadWhileCmd = string.Join(" ", args);
            do
            {
                try
                {
                    var data = Resp3Helper.Read<object>(Stream).Value;
                    ondata?.Invoke(data);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    if (IsConnected) throw;
                    break;
                }
            } while (next());
            _CallReadWhileCmd = null;
        }

        #region Commands Pub/Sub
		public void PSubscribe(string pattern, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_CallReadWhileCmd)) CallReadWhile(onData, () => IsConnected, "PSUBSCRIBE", null, pattern);
			else CallWriteOnly("PSUBSCRIBE", null, pattern);
		}
		public void PSubscribe(string[] pattern, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_CallReadWhileCmd)) CallReadWhile(onData, () => IsConnected, "PSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
			else CallWriteOnly("PSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
		}
		public RedisResult<long> Publish(string channel, string message) => Call<long>("PUBLISH", channel, message);
		public RedisResult<string[]> PubSubChannels(string pattern) => Call<string[]>("PUBSUB", "CHANNELS", pattern);
		public RedisResult<string[]> PubSubNumSub(params string[] channels) => Call<string[]>("PUBSUB", "NUMSUB", "".AddIf(true, channels).ToArray());
		public RedisResult<long> PubSubNumPat() => Call<long>("PUBLISH", "NUMPAT");
		public void PUnSubscribe(params string[] pattern) => CallWriteOnly("PUNSUBSCRIBE", null, "".AddIf(true, pattern).ToArray());
		public void Subscribe(string channel, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_CallReadWhileCmd)) CallReadWhile(onData, () => IsConnected, "SUBSCRIBE", null, channel);
			else CallWriteOnly("SUBSCRIBE", null, channel);
		}
		public void Subscribe(string[] channels, Action<object> onData)
		{
			if (string.IsNullOrWhiteSpace(_CallReadWhileCmd)) CallReadWhile(onData, () => IsConnected, "SUBSCRIBE", null, "".AddIf(true, channels).ToArray());
			else CallWriteOnly("SUBSCRIBE", null, "".AddIf(true, channels).ToArray());
		}
		public void UnSubscribe(params string[] channels) => CallWriteOnly("UNSUBSCRIBE", null, "".AddIf(true, channels).ToArray());
		#endregion

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

        protected void SafeReleaseSocket()
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
            SafeReleaseSocket();
            if (string.IsNullOrWhiteSpace(host?.Trim()))
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
                Ip = string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
                Port = 6379;
                return;
            }
            if (spt.Length == 2) //ipv4:port or domain:port
            {
                if (int.TryParse(spt.Last().Trim(), out var testPort2))
                {
                    Ip = string.IsNullOrWhiteSpace(spt[0].Trim()) == false ? spt[0].Trim() : "127.0.0.1";
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

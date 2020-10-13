using FreeRedis.Internal.IO;
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
        protected abstract RedisSocket Socket { get; }
        protected RedisProtocol Protocol { get; set; } = RedisProtocol.RESP2;
        protected Encoding Encoding { get; set; } = Encoding.UTF8;

        protected RedisProtocol _protocol { get; }
        protected Encoding _encoding { get; }
        protected ClientStatus _state;
        protected Queue<Func<object>> _pipeParses = new Queue<Func<object>>();

        protected T2 Call<T2>(CommandBuilder cb, Func<RedisResult<T2>, T2> parse) => Call<T2, T2>(cb, parse);
        protected T2 Call<T1, T2>(CommandBuilder cb, Func<RedisResult<T1>, T2> parse)
        {
            Socket.Write(Protocol, Encoding, cb);
            switch (_state)
            {
                case ClientStatus.ClientReplyOff:
                case ClientStatus.ClientReplySkip: //CLIENT REPLY ON|OFF|SKIP
                    return default(T2);
                case ClientStatus.Pipeline:
                    _pipeParses.Enqueue(() =>
                    {
                        var rt = Socket.Read<T1>(Encoding);
                        return parse(rt);
                    });
                    return default(T2);
                case ClientStatus.ReadWhile:
                    return default(T2);
                case ClientStatus.Transaction:
                     return default(T2);
            }
            var result = Socket.Read<T1>(Encoding);
            return parse(result);
        }
        protected void CallWriteOnly(CommandBuilder cb)
        {
            Socket.Write(Protocol, Encoding, cb);
        }
        protected void CallReadWhile(Action<object> ondata, Func<bool> next, CommandBuilder cb)
        {
            Socket.Write(Protocol, cb);
            _state = ClientStatus.ReadWhile;
            try
            {
                do
                {
                    try
                    {
                        var data = Socket.Read<object>().Value;
                        ondata?.Invoke(data);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                        if (Socket.IsConnected) throw;
                        break;
                    }
                } while (next());
            }
            finally
            {
                _state = ClientStatus.Normal;
            }
        }

        #region Commands Pub/Sub
		public void PSubscribe(string pattern, Action<object> onData)
		{
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal) CallReadWhile(onData, () => Socket.IsConnected, cb);
			else CallWriteOnly(cb);
		}
		public void PSubscribe(string[] pattern, Action<object> onData)
		{
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal) CallReadWhile(onData, () => Socket.IsConnected, cb);
			else CallWriteOnly(cb);
		}
		public long Publish(string channel, string message) => Call<long>("PUBLISH".Input(channel, message).FlagKey(channel), rt => rt.ThrowOrValue());
		public string[] PubSubChannels(string pattern) => Call<string[]>("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue());
        public string[] PubSubNumSub(params string[] channels) => Call<string[]>("PUBSUB".SubCommand("NUMSUB").Input(channels).FlagKey(channels), rt => rt.ThrowOrValue());
        public long PubSubNumPat() => Call<long>("PUBLISH".SubCommand("NUMPAT"), rt => rt.ThrowOrValue());
		public void PUnSubscribe(params string[] pattern) => CallWriteOnly("PUNSUBSCRIBE".Input(pattern));
		public void Subscribe(string channel, Action<object> onData)
		{
            var cb = "SUBSCRIBE".Input(channel).FlagKey(channel);
            if (_state == ClientStatus.Normal) CallReadWhile(onData, () => Socket.IsConnected, cb);
			else CallWriteOnly(cb);
		}
		public void Subscribe(string[] channels, Action<object> onData)
		{
            var cb = "SUBSCRIBE".Input(channels).FlagKey(channels);
            if (_state == ClientStatus.Normal) CallReadWhile(onData, () => Socket.IsConnected, cb);
			else CallWriteOnly(cb);
		}
		public void UnSubscribe(params string[] channels) => CallWriteOnly("UNSUBSCRIBE".Input(channels).FlagKey(channels));
		#endregion

        public void Release()
        {
            Socket?.SafeReleaseSocket();
            _state = ClientStatus.Normal;
            _pipeParses.Clear();
        }
    }
}

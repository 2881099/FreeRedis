//using FreeRedis.Internal;
//using System;
//using System.Collections.Generic;
//using System.Collections.Concurrent;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace FreeRedis
//{
//    partial class RedisClient
//    {

//#if isasync
//        #region async (copy from sync)
//        /// <summary>
//        /// redis 7.0 shard pub/sub
//        /// </summary>
//        public Task<long> SPublishAsync(string shardchannel, string message) => CallAsync("SPUBLISH".InputKey(shardchannel).Input(message), rt => rt.ThrowOrValue<long>());
//        public Task<string[]> PubSubShardChannelsAsync(string pattern = "*") => CallAsync("PUBSUB".SubCommand("SHARDCHANNELS").Input(pattern), rt => rt.ThrowOrValue<string[]>());
//        public Task<long> PubSubShardNumSubAsync(string channel) => CallAsync("PUBSUB".SubCommand("SHARDNUMSUB").InputKey(channel), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).FirstOrDefault()));
//        public Task<long[]> PubSubShardNumSubAsync(string[] channels) => CallAsync("PUBSUB".SubCommand("SHARDNUMSUB").InputKey(channels), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).ToArray()));
//        #endregion
//#endif

//        /// <summary>
//        /// redis 7.0 shard pub/sub
//        /// </summary>
//        public long SPublish(string shardchannel, string message) => Call("SPUBLISH".InputKey(shardchannel).Input(message), rt => rt.ThrowOrValue<long>());
//        public string[] PubSubShardChannels(string pattern = "*") => Call("PUBSUB".SubCommand("SHARDCHANNELS").Input(pattern), rt => rt.ThrowOrValue<string[]>());
//        public long PubSubShardNumSub(string channel) => Call("PUBSUB".SubCommand("SHARDNUMSUB").InputKey(channel), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).FirstOrDefault()));
//        public long[] PubSubShardNumSub(string[] channels) => Call("PUBSUB".SubCommand("SHARDNUMSUB").InputKey(channels), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).ToArray()));

//        /// <summary>
//        /// redis 7.0 shard pub/sub
//        /// </summary>
//        public IDisposable SSubscribe(string shardchannel, Action<string, object> handler)
//        {
//            if (string.IsNullOrEmpty(shardchannel)) throw new ArgumentNullException(nameof(shardchannel));
//            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
//            return _pubsub.Subscribe(false, true, new[] { shardchannel }, (p, k, d) => handler(k, d));
//        }
//        /// <summary>
//        /// redis 7.0 shard pub/sub
//        /// </summary>
//        public void SUnSubscribe(string shardchannel) => _pubsub.UnSubscribe(false, true, new[] { shardchannel });
//    }
//}
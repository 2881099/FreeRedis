using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
        //public RedisClient PSubscribe(string pattern, Action<object> onData)
        //{
        //    var cb = "PSUBSCRIBE".Input(pattern);
        //    if (_state == ClientStatus.Normal)
        //    {
        //        IRedisSocket rds = null;
        //        rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
        //        return rds.Client;
        //    }
        //    else GetRedisSocket(cb).Write(cb);
        //    return this;
        //}
        //public RedisClient PSubscribe(string[] pattern, Action<object> onData)
        //{
        //    var cb = "PSUBSCRIBE".Input(pattern);
        //    if (_state == ClientStatus.Normal)
        //    {
        //        IRedisSocket rds = null;
        //        rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
        //        return rds.Client;
        //    }
        //    else GetRedisSocket(cb).Write(cb);
        //    return this;
        //}
        //public long Publish(string channel, string message) => Call<long>("PUBLISH".Input(channel, message).FlagKey(channel), rt => rt.ThrowOrValue());
        //public string[] PubSubChannels(string pattern) => Call<string[]>("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue());
        //public string[] PubSubNumSub(params string[] channels) => Call<string[]>("PUBSUB".SubCommand("NUMSUB").Input(channels).FlagKey(channels), rt => rt.ThrowOrValue());
        //public long PubSubNumPat() => Call<long>("PUBLISH".SubCommand("NUMPAT"), rt => rt.ThrowOrValue());
        //public void PUnSubscribe(params string[] pattern)
        //{
        //    var cb = "PUNSUBSCRIBE".Input(pattern);
        //    GetRedisSocket(cb).Write(cb);
        //    _state = ClientStatus.Normal;
        //}
        //public RedisClient Subscribe(string channel, Action<object> onData)
        //{
        //    var cb = "SUBSCRIBE".Input(channel).FlagKey(channel);
        //    if (_state == ClientStatus.Normal)
        //    {
        //        IRedisSocket rds = null;
        //        rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
        //        return rds.Client;
        //    }
        //    else GetRedisSocket(cb).Write(cb);
        //    return this;
        //}
        //public RedisClient Subscribe(string[] channels, Action<object> onData)
        //{
        //    var cb = "SUBSCRIBE".Input(channels).FlagKey(channels);
        //    if (_state == ClientStatus.Normal)
        //    {
        //        IRedisSocket rds = null;
        //        rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
        //        return rds.Client;
        //    }
        //    else GetRedisSocket(cb).Write(cb);
        //    return this;
        //}
        //public void UnSubscribe(params string[] channels)
        //{
        //    var cb = "UNSUBSCRIBE".Input(channels).FlagKey(channels);
        //    GetRedisSocket(cb).Write(cb);
        //    _state = ClientStatus.Normal;
        //}
    }
}

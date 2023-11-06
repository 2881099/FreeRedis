using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using FreeRedis.Internal.ObjectPool;

namespace FreeRedis
{
    partial class RedisClient
    {
        public SubscribeStreamObject SubscribeStream(string streamKey, Action<Dictionary<string, string>> onMessage)
        {
            if (string.IsNullOrEmpty(streamKey)) throw new ArgumentException("Parameter streamKey cannot be empty");
            var redis = this;
            var subobj = new SubscribeStreamObject();

            var groupName = "FreeRedis__group";
            var consumerName = "FreeRedis__consumer";
            void CreateGroupAndConsumer()
            {
                using (var loc1 = redis.StartPipe())
                {
                    loc1.XGroupCreate(streamKey, groupName, "$", true);
                    loc1.XGroupCreateConsumer(streamKey, groupName, consumerName);
                    loc1.EndPipe();
                }
            }

            if (redis.Exists(streamKey) == false) CreateGroupAndConsumer();
            else
            {
                if (redis.Type(streamKey) != KeyType.stream) throw new ArgumentException($"'{streamKey}' type is not STREAM");
                if (redis.XInfoGroups(streamKey).Any(a => a.name == groupName) == false) CreateGroupAndConsumer();
                else if (redis.XInfoConsumers(streamKey, groupName).Any(a => a.name == consumerName) == false) redis.XGroupCreateConsumer(streamKey, groupName, consumerName);
            }

            TestTrace.WriteLine($"Subscribing to stream(streamKey:{streamKey})", ConsoleColor.DarkGreen);
            new Thread(() =>
            {
                while (subobj.IsUnsubscribed == false)
                {
                    try
                    {
                        //var result = redis.XReadGroup(groupName, consumerName, 5000, streamKey, ">");
                        var result = Call("XREADGROUP"
                            .Input("GROUP", groupName, consumerName)
                            .Input("COUNT", 1)
                            .Input("BLOCK", 5000)
                            .InputRaw("STREAMS")
                            .InputKey(streamKey)
                            .Input(">"), rt =>
                            {
                                if (rt.IsError)
                                {
                                    if (
                                        rt.SimpleError == $"UNBLOCKED the stream key no longer exists" ||
                                        rt.SimpleError == $"NOGROUP No such key '{streamKey}' or consumer group '{groupName}' in XREADGROUP with GROUP option")
                                    {
                                        CreateGroupAndConsumer();
                                        return null;
                                    }
                                }
                                return rt.ThrowOrValueToXRead();
                            })?.FirstOrDefault()?.entries?.FirstOrDefault();
                        if (result != null)
                        {
                            onMessage?.Invoke(result.fieldValues?.MapToHash<string>(Encoding.UTF8));
                            redis.XAck(streamKey, groupName, result.id);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        TestTrace.WriteLine($"Stream subscription error(streamKey:{streamKey}): {ex.Message}", ConsoleColor.DarkRed);

                        Thread.CurrentThread.Join(3000);
                    }
                }
            }).Start();

            AppDomain.CurrentDomain.ProcessExit += (s1, e1) =>
            {
                subobj.Dispose();
            };
            try
            {
                Console.CancelKeyPress += (s1, e1) =>
                {
                    if (e1.Cancel) return;
                    subobj.Dispose();
                };
            }
            catch { }

            return subobj;
        }
    }
}

namespace FreeRedis.Internal
{
    public class SubscribeStreamObject : IDisposable
    {
        internal List<SubscribeStreamObject> OtherSubs = new List<SubscribeStreamObject>();
        public bool IsUnsubscribed { get; set; }

        public void Dispose()
        {
            this.IsUnsubscribed = true;
            foreach (var sub in OtherSubs) sub.Dispose();
        }
    }
}
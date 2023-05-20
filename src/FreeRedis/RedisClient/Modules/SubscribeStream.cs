using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        public SubscribeStreamObject SubscribeStream(string streamKey, Action<Dictionary<string, string>> onMessage)
        {
            if (string.IsNullOrEmpty(streamKey)) throw new ArgumentException("Parameter streamKey cannot be empty");
            var subobj = new SubscribeStreamObject();

            var groupName = "__FreeRedis__SubscribeStream__group";
            var consumerName = "__FreeRedis__SubscribeStream__consumer";
            if (this.XInfoGroups(streamKey).Any(a => a.name == streamKey) == false) this.XGroupCreate(streamKey, groupName, "$", true);
            if (this.XInfoConsumers(streamKey, groupName).Any(a => a.name == consumerName) == false) this.XGroupCreateConsumer(streamKey, groupName, consumerName);
            var bgcolor = Console.BackgroundColor;
            var forecolor = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Subscribing to stream(streamKey:{streamKey})");
            Console.BackgroundColor = bgcolor;
            Console.ForegroundColor = forecolor;
            Console.WriteLine();

            new Thread(() =>
            {
                while (subobj.IsUnsubscribed == false)
                {
                    try
                    {
                        var result = this.XReadGroup(groupName, consumerName, 5, streamKey, "$");
                        if (result != null)
                        {
                            onMessage?.Invoke(result.fieldValues?.MapToHash<string>(Encoding.UTF8));
                            this.XAck(streamKey, groupName, result.id);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        bgcolor = Console.BackgroundColor;
                        forecolor = Console.ForegroundColor;
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"Stream subscription error(streamKey:{streamKey}): {ex.Message}");
                        Console.BackgroundColor = bgcolor;
                        Console.ForegroundColor = forecolor;
                        Console.WriteLine();

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
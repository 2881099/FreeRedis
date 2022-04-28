using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public class SubscribeListObject : IDisposable
    {
        internal List<SubscribeListObject> OtherSubs = new List<SubscribeListObject>();
        public bool IsUnsubscribed { get; set; }

        public void Dispose()
        {
            this.IsUnsubscribed = true;
            foreach (var sub in OtherSubs) sub.Dispose();
        }
    }
    public class SubscribeListBroadcastObject : IDisposable
    {
        internal Action OnDispose;
        internal List<SubscribeListObject> SubscribeLists = new List<SubscribeListObject>();

        public void Dispose()
        {
            try { OnDispose?.Invoke(); } catch (ObjectDisposedException) { }
            foreach (var sub in SubscribeLists) sub.Dispose();
        }
    }
}

namespace FreeRedis
{
    partial class RedisClient
    {
        /// <summary>
        /// 使用lpush + blpop订阅端（多端争抢模式），只有一端收到消息
        /// </summary>
        /// <param name="listKey">list key（不含prefix前辍）</param>
        /// <param name="onMessage">接收消息委托</param>
        /// <returns></returns>
        public SubscribeListObject SubscribeList(string listKey, Action<string> onMessage) => SubscribeList(new[] { listKey }, (k, v) => onMessage(v), false);
        /// <summary>
        /// 使用lpush + blpop订阅端（多端争抢模式），只有一端收到消息
        /// </summary>
        /// <param name="listKeys">支持多个 key（不含prefix前辍）</param>
        /// <param name="onMessage">接收消息委托，参数1：key；参数2：消息体</param>
        /// <returns></returns>
        public SubscribeListObject SubscribeList(string[] listKeys, Action<string, string> onMessage) => SubscribeList(listKeys, onMessage, false);
        private SubscribeListObject SubscribeList(string[] listKeys, Action<string, string> onMessage, bool ignoreEmpty)
        {
            if (listKeys == null || listKeys.Any() == false) throw new ArgumentException("Parameter listkeys cannot be empty");
            var listKeysStr = string.Join(", ", listKeys);
            var isMultiKey = listKeys.Length > 1;
            var subobj = new SubscribeListObject();

            var bgcolor = Console.BackgroundColor;
            var forecolor = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Subscribing to list(listKey:{listKeysStr})");
            Console.BackgroundColor = bgcolor;
            Console.ForegroundColor = forecolor;
            Console.WriteLine();

            new Thread(() =>
            {
                while (subobj.IsUnsubscribed == false)
                {
                    try
                    {
                        if (isMultiKey)
                        {
                            var msg = this.BLPop(listKeys, 5);
                            if (msg != null)
                                if (!ignoreEmpty || (ignoreEmpty && !string.IsNullOrEmpty(msg.value)))
                                    onMessage?.Invoke(msg.key, msg.value);
                        }
                        else
                        {
                            var msg = this.BLPop(listKeys[0], 5);
                            if (!ignoreEmpty || (ignoreEmpty && !string.IsNullOrEmpty(msg)))
                                onMessage?.Invoke(listKeys[0], msg);
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
                        Console.Write($"List subscription error(listKey:{listKeysStr}): {ex.Message}");
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

        /// <summary>
        /// 使用lpush + blpop订阅端（多端非争抢模式），都可以收到消息
        /// </summary>
        /// <param name="listKey">list key（不含prefix前辍）</param>
        /// <param name="clientId">订阅端标识，若重复则争抢，若唯一必然收到消息</param>
        /// <param name="onMessage">接收消息委托</param>
        /// <returns></returns>
        public SubscribeListBroadcastObject SubscribeListBroadcast(string listKey, string clientId, Action<string> onMessage)
        {
            this.HSetNx($"{listKey}_SubscribeListBroadcast", clientId, 1);
            var subobj = new SubscribeListBroadcastObject
            {
                OnDispose = () =>
                {
                    this.HDel($"{listKey}_SubscribeListBroadcast", clientId);
                }
            };
            //订阅其他端转发的消息
            subobj.SubscribeLists.Add(this.SubscribeList($"{listKey}_{clientId}", onMessage));
            //订阅主消息，接收消息后分发
            subobj.SubscribeLists.Add(this.SubscribeList(new[] { listKey }, (key, msg) =>
            {
                try
                {
                    this.HSetNx($"{listKey}_SubscribeListBroadcast", clientId, 1);
                    if (msg == null) return;

                    var clients = this.HKeys($"{listKey}_SubscribeListBroadcast");
                    var pipe = this.StartPipe();
                    foreach (var c in clients)
                        if (string.Compare(clientId, c, true) != 0) //过滤本端分发
                            pipe.LPush($"{listKey}_{c}", msg);
                    pipe.EndPipe();
                    onMessage?.Invoke(msg);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    var bgcolor = Console.BackgroundColor;
                    var forecolor = Console.ForegroundColor;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"List subscription error(listKey:{listKey}): {ex.Message}");
                    Console.BackgroundColor = bgcolor;
                    Console.ForegroundColor = forecolor;
                    Console.WriteLine();
                }
            }, true));

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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class PubSubTests : TestBase
    {
        [Fact]
        public void PSubscribe()
        {
            //see Subscribe
        }

        [Fact]
        public void Publish()
        {
            var key1 = "Publish1";
            Assert.Equal(0, cli.Publish(key1, "test"));
        }

        [Fact]
        public void PubSubChannels()
        {
            var key1 = "PubSubChannels1";
            using (cli.Subscribe(key1, (chan, msg) =>
            {

            }))
            {
                var chans = cli.PubSubChannels("PubSubChannels1*");
                Assert.Single(chans);
                Assert.Equal(key1, chans[0]);
                Thread.CurrentThread.Join(500);
            }
        }

        [Fact]
        public void PubSubNumSub()
        {
            var key1 = "PubSubNumSub1";
            using (cli.Subscribe(key1, (chan, msg) =>
            {

            }))
            {
                var r1 = cli.PubSubNumSub("PubSubNumSub1");
                Assert.Equal(1, r1);

                var r2 = cli.PubSubNumSub(new[] { "PubSubNumSub1" });
                Assert.Single(r2);
                Assert.Equal(1, r2[0]);

                Thread.CurrentThread.Join(500);
            }
        }

        [Fact]
        public void PubSubNumPat()
        {
            cli.PubSubNumPat("123");
        }

        [Fact]
        public void PUnSubscribe()
        {
        }

        [Fact]
        public void Subscribe()
        {
            var key1 = "Subscribe1";
            var key2 = "Subscribe2";

            bool isbreak = false;
            new Thread(() =>
            {
                while (isbreak == false)
                {
                    cli.Publish(key1, Guid.NewGuid().ToString());
                    cli.Publish(key2, Guid.NewGuid().ToString());
                    cli.Publish("randomSubscribe1", Guid.NewGuid().ToString());
                    Thread.CurrentThread.Join(100);
                }
            }).Start();

            using (cli.Subscribe(key1, ondata))
            {
                using (cli.Subscribe(key2, ondata))
                {
                    using (cli.PSubscribe("*", ondata))
                    {
                        Thread.CurrentThread.Join(2000);
                    }
                    Thread.CurrentThread.Join(2000);
                }
                Thread.CurrentThread.Join(2000);
            }
            Trace.WriteLine("one more time");
            using (cli.Subscribe(key1, ondata))
            {
                using (cli.Subscribe(key2, ondata))
                {
                    using (cli.PSubscribe("*", ondata))
                    {
                        Thread.CurrentThread.Join(2000);
                    }
                    Thread.CurrentThread.Join(2000);
                }
                Thread.CurrentThread.Join(2000);
            }
            void ondata(string channel, object data)
            {
                Trace.WriteLine($"{channel} -> {data}");
            }
            isbreak = true;
        }

        [Fact]
        public void UnSubscribe()
        {
        }

    }
}

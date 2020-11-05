using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class ScriptingTests : TestBase
    {
        [Fact]
        public void Eval()
        {
            using (var sh = cli.GetDatabase())
            {
                var r1 = sh.Eval("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}", new[] { "key1", "key2" }, "first", "second") as object[];
                Assert.NotNull(r1);
                Assert.True(r1.Length == 4);
                Assert.Equal("key1", r1[0]);
                Assert.Equal("key2", r1[1]);
                Assert.Equal("first", r1[2]);
                Assert.Equal("second", r1[3]);
                Assert.Equal("OK", sh.Eval($"return redis.call('set','{Guid.NewGuid()}','bar')"));
                Assert.Equal("OK", sh.Eval("return redis.call('set',KEYS[1],'bar')", new[] { Guid.NewGuid().ToString() }));

                //RESP3
                Assert.Equal(10L, sh.Eval("return 10"));
                var r2 = sh.Eval("return {1,2,{3,'Hello World!'}}") as object[];
                Assert.NotNull(r2);
                Assert.True(r2.Length == 3);
                Assert.Equal(1L, r2[0]);
                Assert.Equal(2L, r2[1]);
                var r3 = r2[2] as object[];
                Assert.Equal(3L, r3[0]);
                Assert.Equal("Hello World!", r3[1]);

                var r4 = sh.Eval("return {1,2,3.3333,somekey='somevalue','foo',nil,'bar'}") as object[];
                //As you can see 3.333 is converted into 3, somekey is excluded, and the bar string is never returned as there is a nil before.
                Assert.NotNull(r4);
                Assert.True(r4.Length == 4);
                Assert.Equal(1L, r4[0]);
                Assert.Equal(2L, r4[1]);
                Assert.Equal(3L, r4[2]);
                Assert.Equal("foo", r4[3]);

                Assert.Equal("My Error", Assert.Throws<RedisServerException>(() => sh.Eval("return {err=\"My Error\"}"))?.Message);
                Assert.Equal("My Error222", Assert.Throws<RedisServerException>(() => sh.Eval("return redis.error_reply(\"My Error222\")"))?.Message);

                var key1 = Guid.NewGuid().ToString();
                Assert.Equal(1, sh.LPush(key1, "a"));
                Assert.True(Assert.Throws<RedisServerException>(() => sh.Eval($"return redis.call('get','{key1}')"))?.Message.Contains("ERR Error running script (call to ") == true);
                //(error) ERR Error running script (call to f_6b1bf486c81ceb7edf3c093f4c48582e38c0e791): ERR Operation against a key holding the wrong kind of value
            }
        }

        [Fact]
        public void EvalSha()
        {
            var scriptid = cli.ScriptLoad("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}");
            Assert.True(!string.IsNullOrWhiteSpace(scriptid));

            var r1 = cli.EvalSha(scriptid, new[] { "key1", "key2" }, "first", "second") as object[];
            Assert.NotNull(r1);
            Assert.True(r1.Length == 4);
            Assert.Equal("key1", r1[0]);
            Assert.Equal("key2", r1[1]);
            Assert.Equal("first", r1[2]);
            Assert.Equal("second", r1[3]);
        }

        [Fact]
        public void ScriptExists()
        {
            cli.ScriptFlush();
            var r1 = cli.ScriptLoad("return redis.call('get','foo')");
            Assert.True(!string.IsNullOrWhiteSpace(r1));
            Assert.True(cli.ScriptExists(r1));
            Assert.False(cli.ScriptExists("6b1bf486c81ceb7edf3c193f4c48582e38c0e791"));

            var r2 = cli.ScriptLoad("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}");
            Assert.True(!string.IsNullOrWhiteSpace(r2));
            Assert.True(cli.ScriptExists(r2));

            var r3 = cli.ScriptExists(new[] { r1, "6b1bf486c81ceb7edf3c193f4c48582e38c0e791", r2 });
            Assert.Equal(3, r3.Length);
            Assert.True(r3[0]);
            Assert.False(r3[1]);
            Assert.True(r3[2]);

            cli.ScriptFlush();
            Assert.False(cli.ScriptExists(r1));
            Assert.False(cli.ScriptExists("6b1bf486c81ceb7edf3c193f4c48582e38c0e791"));
            Assert.False(cli.ScriptExists(r2)); 
            r3 = cli.ScriptExists(new[] { r1, "6b1bf486c81ceb7edf3c193f4c48582e38c0e791", r2 });
            Assert.Equal(3, r3.Length);
            Assert.False(r3[0]);
            Assert.False(r3[1]);
            Assert.False(r3[2]);
        }

        [Fact]
        public void ScriptFlush()
        {
            //cli.ScriptFlush();
        }

        [Fact]
        public void ScriptKill()
        {
            Assert.Equal("NOTBUSY No scripts in execution right now.", Assert.Throws<RedisServerException>(() => cli.ScriptKill())?.Message);
        }

        [Fact]
        public void ScriptLoad()
        {
            var r1 = cli.ScriptLoad("return redis.call('get','foo')");
            Assert.True(!string.IsNullOrWhiteSpace(r1));
        }

    }
}

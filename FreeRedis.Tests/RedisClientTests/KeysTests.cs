using FreeRedis.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class KeysTests : TestBase
    {
        [Fact]
        public void Del()
        {
            var keys = Enumerable.Range(0, 10).Select(a => Guid.NewGuid().ToString()).ToArray();
            cli.MSet(keys.ToDictionary(a => a, a => (object)a));
            Assert.Equal(10, cli.Del(keys));
        }

        [Fact]
        public void Dump()
        {
            cli.MSet("TestDump_null1", base.Null, "TestDump_string1", base.String, "TestDump_bytes1", base.Bytes, "TestDump_class1", base.Class);

            cli.Restore("TestDump_null2", cli.Dump("TestDump_null1"));
            Assert.Equal(cli.Get("TestDump_null2"), cli.Get("TestDump_null1"));

            cli.Restore("TestDump_string2", cli.Dump("TestDump_string1"));
            Assert.Equal(cli.Get("TestDump_string2"), cli.Get("TestDump_string1"));

            cli.Restore("TestDump_bytes2", cli.Dump("TestDump_bytes1"));
            Assert.Equal(cli.Get<byte[]>("TestDump_bytes2"), cli.Get<byte[]>("TestDump_bytes1"));

            cli.Restore("TestDump_class2", cli.Dump("TestDump_class1"));
            Assert.Equal(cli.Get<TestClass>("TestDump_class2").ToString(), cli.Get<TestClass>("TestDump_class1").ToString());
        }

        [Fact]
        public void Exists()
        {
            cli.Del("TestExists_null1");
            Assert.False(cli.Exists("TestExists_null1"));
            cli.Set("TestExists_null1", 1);
            Assert.True(cli.Exists("TestExists_null1"));
            Assert.Equal(1, cli.Del("TestExists_null1"));
            Assert.False(cli.Exists("TestExists_null1"));
        }

        [Fact]
        public void Expire()
        {
            cli.MSet("TestExpire_null1", base.Null, "TestExpire_string1", base.String, "TestExpire_bytes1", base.Bytes, "TestExpire_class1", base.Class);

            Assert.True(cli.Expire("TestExpire_null1", 10));
            Assert.Equal(10, cli.Ttl("TestExpire_null1"));
            Assert.True(cli.Expire("TestExpire_string1", 60 * 60));
            Assert.Equal(60 * 60, cli.Ttl("TestExpire_string1"));
        }

        [Fact]
        public void ExpireAt()
        {
            cli.MSet("TestExpireAt_null1", base.Null, "TestExpireAt_string1", base.String, "TestExpireAt_bytes1", base.Bytes, "TestExpireAt_class1", base.Class);

            Assert.True(cli.ExpireAt("TestExpireAt_null1", DateTime.UtcNow.AddSeconds(10)));
            Assert.InRange(cli.Ttl("TestExpireAt_null1"), 9, 20);
            Assert.True(cli.ExpireAt("TestExpireAt_string1", DateTime.UtcNow.AddHours(1)));
            Assert.InRange(cli.Ttl("TestExpireAt_string1"), 60 * 60 - 10, 60 * 60 + 10);
        }

        [Fact]
        public void Keys()
        {
            cli.MSet("TestKeys_null1", base.Null, "TestKeys_string1", base.String, "TestKeys_bytes1", base.Bytes, "TestKeys_class1", base.Class);
            Assert.Equal(4, cli.Keys("TestKeys_*").Length);
        }

        [Fact]
        public void Migrate()
        {

        }

        [Fact]
        public void Move()
        {
            cli.MSet("TestMove_null1", base.Null, "TestMove_string1", base.String, "TestMove_bytes1", base.Bytes, "TestMove_class1", base.Class);

            Assert.True(cli.Move("TestMove_string1", 1));
            Assert.False(cli.Exists("TestMove_string1"));

            using (var tran = cli.Multi())
            {
                tran.Select(1);
                Assert.Equal(base.String, tran.Value.Get("TestMove_string1"));
                tran.Value.Select(2);
            }

            Assert.True(rds.Set("TestMove_string1", base.String));
            Assert.False(rds.Move("TestMove_string1", 1));
            Assert.Equal(base.String, rds.Get("TestMove_string1"));

            using (var conn = rds.Nodes.First().Value.Get())
            {
                conn.Value.Select(1);
                Assert.Equal(base.String, conn.Value.Get("TestMove_string1"));
                conn.Value.Select(2);
            }
        }

        [Fact]
        public void ObjectRefCount()
        {

        }

        [Fact]
        public void ObjectIdleTime()
        {

        }

        [Fact]
        public void ObjectEncoding()
        {

        }

        [Fact]
        public void ObjectFreq()
        {

        }

        [Fact]
        public void ObjectHelp()
        {

        }

        [Fact]
        public void Presist()
        {

        }

        [Fact]
        public void PExpire()
        {

        }

        [Fact]
        public void PExpireAt()
        {

        }

        [Fact]
        public void PTtl()
        {

        }

        [Fact]
        public void RandomKey()
        {

        }

        [Fact]
        public void Rename()
        {

        }

        [Fact]
        public void RenameNx()
        {

        }

        [Fact]
        public void Restore()
        {

        }

        [Fact]
        public void Scan()
        {

        }

        [Fact]
        public void Sort()
        {

        }

        [Fact]
        public void Touch()
        {

        }

        [Fact]
        public void Ttl()
        {

        }

        [Fact]
        public void Type()
        {

        }

        [Fact]
        public void UnLink()
        {

        }

        [Fact]
        public void Wait()
        {

        }
    }
}

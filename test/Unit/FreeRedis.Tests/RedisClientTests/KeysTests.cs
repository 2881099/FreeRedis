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
            cli.Del("TestDump_null2", "TestDump_string2", "TestDump_bytes2", "TestDump_class2");
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

            using (var sh = cli.GetDatabase(11))
            {
                sh.Del("TestMove_string1");
            }

            Assert.True(cli.Move("TestMove_string1", 11));
            Assert.False(cli.Exists("TestMove_string1"));

            using (var sh = cli.GetDatabase(11))
            {
                Assert.Equal(base.String, sh.Get("TestMove_string1"));
            }

            cli.Set("TestMove_string1", base.String);
            Assert.False(cli.Move("TestMove_string1", 11)); //target exists
            Assert.Equal(base.String, cli.Get("TestMove_string1"));

            using (var sh = cli.GetDatabase(11))
            {
                Assert.Equal(base.String, sh.Get("TestMove_string1"));
                sh.Del("TestMove_string1");
            }
        }

        [Fact]
        public void ObjectRefCount()
        {
            cli.MSet("TestObjectRefCount_null1", base.Null, "TestObjectRefCount_string1", base.String, "TestObjectRefCount_bytes1", base.Bytes, "TestObjectRefCount_class1", base.Class);
            Assert.True(cli.Exists("TestObjectRefCount_string1"));
            cli.Get("TestObjectRefCount_string1");

            Assert.Null(cli.ObjectRefCount("TestObjectRefCount_bytes11"));
            Assert.Equal(1, cli.ObjectRefCount("TestObjectRefCount_string1"));
        }

        [Fact]
        public void ObjectIdleTime()
        {
            var key1 = "ObjectIdleTime1";
            cli.LPush(key1, "hello world");

            Assert.Equal(0, cli.ObjectIdleTime(key1));
        }

        [Fact]
        public void ObjectEncoding()
        {
            var key1 = "ObjectEncoding1";
            cli.LPush(key1, "ObjectEncoding1_val1");
            Assert.Equal("quicklist", cli.ObjectEncoding(key1));
        }

        [Fact]
        public void ObjectFreq()
        {
            var key1 = "ObjectFreq1";
            cli.Set(key1, "test1");
            cli.Get(key1);

            //Assert.True(cli.ObjectFreq(key1) > 0);
            Assert.Null(cli.ObjectFreq(key1 + "_no_such_key"));
        }

        [Fact]
        public void Presist()
        {
            cli.MSet("TestPersist_null1", base.Null, "TestPersist_string1", base.String, "TestPersist_bytes1", base.Bytes, "TestPersist_class1", base.Class);

            Assert.True(cli.Expire("TestPersist_null1", 10));
            Assert.Equal(10, cli.Ttl("TestPersist_null1"));
            Assert.True(cli.Expire("TestPersist_string1", 60 * 60));
            Assert.Equal(60 * 60, cli.Ttl("TestPersist_string1"));

            Assert.True(cli.Persist("TestPersist_null1"));
            Assert.False(cli.Persist("TestPersist_null11"));
            Assert.True(cli.Persist("TestPersist_string1"));
            Assert.False(cli.Persist("TestPersist_string11"));

            Assert.Equal(-1, cli.Ttl("TestPersist_null1"));
            Assert.Equal(-1, cli.Ttl("TestPersist_string1"));
        }

        [Fact]
        public void PExpire()
        {
            cli.MSet("TestPExpire_null1", base.Null, "TestPExpire_string1", base.String, "TestPExpire_bytes1", base.Bytes, "TestPExpire_class1", base.Class);

            Assert.True(cli.PExpire("TestPExpire_null1", 10000));
            //Assert.InRange(cli.PTtl("TestPExpire_null1"), 9000, 10000);
            Assert.True(cli.PExpire("TestPExpire_string1", 60 * 60));
            //Assert.InRange(cli.PTtl("TestPExpire_string1"), 1000 * 60 * 60 - 1000, 1000 * 60 * 60);
        }

        [Fact]
        public void PExpireAt()
        {
            cli.MSet("TestPExpireAt_null1", base.Null, "TestPExpireAt_string1", base.String, "TestPExpireAt_bytes1", base.Bytes, "TestPExpireAt_class1", base.Class);

            Assert.True(cli.ExpireAt("TestPExpireAt_null1", DateTime.UtcNow.AddSeconds(10)));
            Assert.InRange(cli.PTtl("TestPExpireAt_null1"), 9000, 20000);
            Assert.True(cli.ExpireAt("TestPExpireAt_string1", DateTime.UtcNow.AddHours(1)));
            Assert.InRange(cli.PTtl("TestPExpireAt_string1"), 1000 * 60 * 60 - 10000, 1000 * 60 * 60 + 10000);
        }

        [Fact]
        public void PTtl()
        {
            cli.MSet("TestPTtl_null1", base.Null, "TestPTtl_string1", base.String, "TestPTtl_bytes1", base.Bytes, "TestPTtl_class1", base.Class);

            Assert.True(cli.PExpire("TestPTtl_null1", 1000));
            Assert.InRange(cli.PTtl("TestPTtl_null1"), 500, 1000);
            Assert.InRange(cli.PTtl("TestPTtl_null11"), long.MinValue, -1);
        }

        [Fact]
        public void RandomKey()
        {
            cli.MSet("TestRandomKey_null1", base.Null, "TestRandomKey_string1", base.String, "TestRandomKey_bytes1", base.Bytes, "TestRandomKey_class1", base.Class);

            Assert.NotNull(cli.RandomKey());
        }

        [Fact]
        public void Rename()
        {
            cli.MSet("TestRename_null1", base.Null, "TestRename_string1", base.String, "TestRename_bytes1", base.Bytes, "TestRename_class1", base.Class);

            Assert.Equal(base.String, cli.Get("TestRename_string1"));
            cli.Rename("TestRename_string1", "TestRename_string11");
            Assert.False(cli.Exists("TestRename_string1"));
            Assert.Equal(base.String, cli.Get("TestRename_string11"));

            cli.Rename("TestRename_class1", "TestRename_string11");
            Assert.False(cli.Exists("TestRename_class1"));
            Assert.Equal(base.Class.ToString(), cli.Get<TestClass>("TestRename_string11").ToString());
        }

        [Fact]
        public void RenameNx()
        {
            cli.Del("TestRenameNx_string11", "TestRename_string11");
            cli.MSet("TestRenameNx_null1", base.Null, "TestRenameNx_string1", base.String, "TestRenameNx_bytes1", base.Bytes, "TestRenameNx_class1", base.Class);

            Assert.Equal(base.String, cli.Get("TestRenameNx_string1"));
            Assert.True(cli.RenameNx("TestRenameNx_string1", "TestRenameNx_string11"));
            Assert.False(cli.Exists("TestRenameNx_string1"));
            Assert.Equal(base.String, cli.Get("TestRenameNx_string11"));

            Assert.True(cli.RenameNx("TestRenameNx_class1", "TestRename_string11"));
            Assert.False(cli.Exists("TestRenameNx_class1"));
            Assert.Equal(base.Class.ToString(), cli.Get<TestClass>("TestRename_string11").ToString());
        }

        [Fact]
        public void Restore()
        {
            cli.Del("TestRestore_null2", "TestRestore_string2", "TestRestore_bytes2", "TestRestore_class2");
            cli.MSet("TestRestore_null1", base.Null, "TestRestore_string1", base.String, "TestRestore_bytes1", base.Bytes, "TestRestore_class1", base.Class);

            cli.Restore("TestRestore_null2", cli.Dump("TestRestore_null1"));
            Assert.Equal(cli.Get("TestRestore_null2"), cli.Get("TestRestore_null1"));

            cli.Restore("TestRestore_string2", cli.Dump("TestRestore_string1"));
            Assert.Equal(cli.Get("TestRestore_string2"), cli.Get("TestRestore_string1"));

            cli.Restore("TestRestore_bytes2", cli.Dump("TestRestore_bytes1"));
            Assert.Equal(cli.Get<byte[]>("TestRestore_bytes2"), cli.Get<byte[]>("TestRestore_bytes1"));

            cli.Restore("TestRestore_class2", cli.Dump("TestRestore_class1"));
            Assert.Equal(cli.Get<TestClass>("TestRestore_class2").ToString(), cli.Get<TestClass>("TestRestore_class1").ToString());
        }

        [Fact]
        public void Scan()
        {
            for (var a = 0; a < 11; a++)
                cli.Set(Guid.NewGuid().ToString(), a);

            var keys = new List<string>();
            foreach (var rt in cli.Scan("*", 2, null))
            {
                keys.AddRange(rt);
            }
        }
        [Fact]
        public void HScan()
        {
            cli.Del("HScan01");
            for (var a = 0; a < 11; a++)
                cli.HSet("HScan01", Guid.NewGuid().ToString(), a);

            var keys = new List<KeyValuePair<string, string>>();
            foreach (var rt in cli.HScan("HScan01", "*", 2))
            {
                keys.AddRange(rt);
            }
            Assert.Equal(11, keys.Count);
        }

        [Fact]
        public void Sort()
        {
            var key1 = "Sort1";
            cli.LPush(key1, 1, 2, 10);
            cli.Set("bar1", "bar1");
            cli.Set("bar2", "bar2");
            cli.Set("bar10", "bar10");
            cli.Set("car1", "car1");
            cli.Set("car2", "car2");
            cli.Set("car10", "car10");

            var r1 = cli.Sort(key1, getPatterns: new[] { "car*", "bar*" }, collation: Collation.desc, alpha: true);
            var r2 = cli.Sort(key1, getPatterns: new[] { "car*", "bar*" }, offset: 1, count:5, collation: Collation.desc, alpha: true);
        }

        [Fact]
        public void Touch()
        {

        }

        [Fact]
        public void Ttl()
        {
            cli.MSet("TestTtl_null1", base.Null, "TestTtl_string1", base.String, "TestTtl_bytes1", base.Bytes, "TestTtl_class1", base.Class);

            Assert.True(cli.Expire("TestTtl_null1", 10));
            Assert.InRange(cli.Ttl("TestTtl_null1"), 5, 10);
            Assert.InRange(cli.Ttl("TestTtl_null11"), long.MinValue, -1);
        }

        [Fact]
        public void Type()
        {
            cli.MSet("TestType_null1", base.Null, "TestType_string1", base.String, "TestType_bytes1", base.Bytes, "TestType_class1", base.Class);

            Assert.Equal(KeyType.none, cli.Type("TestType_string111111111123"));
            Assert.Equal(KeyType.@string, cli.Type("TestType_string1"));
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

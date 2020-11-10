using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FreeRedis.Tests
{
    public class TestBase
	{
		//static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() => new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1"));
		static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
		{
			//var r = new RedisClient(new ConnectionStringBuilder[] { "127.0.0.1:6379,database=1,password=123" }); //redis 3.2 cluster
			//var r = new RedisClient("127.0.0.1:6379,database=1"); //redis 3.2
			//var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
			var r = new RedisClient("192.168.164.10:6379,database=1,max pool size=10,protocol=RESP2,ClientName=FreeRedis"); //redis 6.0
			r.Serialize = obj => JsonConvert.SerializeObject(obj);
			r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
			r.Notice += (s, e) => Trace.WriteLine(e.Log);
			return r;
		});
		public static RedisClient cli => _cliLazy.Value;

		protected readonly object Null = null;
		protected readonly string String = "我是中国人";
		protected readonly byte[] Bytes = Encoding.UTF8.GetBytes("这是一个byte字节");
		protected readonly TestClass Class = new TestClass { Id = 1, Name = "Class名称", CreateTime = DateTime.Now, TagId = new[] { 1, 3, 3, 3, 3 } };

		public TestBase() {
			//rds.NodesServerManager.FlushAll();
		}
    }

	public class TestClass {
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime CreateTime { get; set; }

		public int[] TagId { get; set; }

		public override string ToString() {
			return JsonConvert.SerializeObject(this);
		}
	}
}

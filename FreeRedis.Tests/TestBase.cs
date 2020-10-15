﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis.Tests
{
    public class TestBase
	{
		static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() => new RedisClient("127.0.0.1:6379"));
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
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}
	}
}

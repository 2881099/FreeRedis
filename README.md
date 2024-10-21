<h1 align="center"> 🦄 FreeRedis </h1>

<div align="center">

FreeRedis is a redis client based on .NET, supports .NET Core 2.1+, .NET Framework 4.0+, Xamarin, and AOT.

[![nuget](https://img.shields.io/nuget/v/FreeRedis.svg?style=flat-square)](https://www.nuget.org/packages/FreeRedis) 
[![stats](https://img.shields.io/nuget/dt/FreeRedis.svg?style=flat-square)](https://www.nuget.org/stats/packages/FreeRedis?groupby=Version) 
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://raw.githubusercontent.com/2881099/FreeRedis/master/LICENSE.txt)

<p>
    <span>English</span> |  
    <a href="README.zh-CN.md">中文</a>
</p>
</div>

- 🌈 RedisClient Keep all method names consistent with redis-cli
- 🌌 Support Redis Cluster (requires redis-server 3.2 and above)
- ⛳ Support Redis Sentinel
- 🎣 Support Redis Master-Slave
- 📡 Support Redis Pub-Sub
- 📃 Support Redis Lua Scripting
- 💻 Support Pipeline、Transaction、DelayQueue、RediSearch
- 🌴 Support Geo type commands (requires redis-server 3.2 and above)
- 🌲 Support Streams type commands (requires redis-server 5.0 and above)
- ⚡ Support Client-side-caching (requires redis-server 6.0 and above)
- 🌳 Support Redis 6 RESP3 Protocol

QQ Groups：4336577(full)、**8578575(available)**、**52508226(available)**

## 🚀 Quick start

```csharp
public static RedisClient cli = new RedisClient("127.0.0.1:6379,password=123,defaultDatabase=13");
cli.Serialize = obj => JsonConvert.SerializeObject(obj);
cli.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
cli.Notice += (s, e) => Console.WriteLine(e.Log); //print command log

cli.Set("key1", "value1");
cli.MSet("key1", "value1", "key2", "value2");

string value1 = cli.Get("key1");
string[] vals = cli.MGet("key1", "key2");
```

> Supports strings, hashes, lists, sets, sorted sets, bitmaps, hyperloglogs, geo, streams And BloomFilter.

| Parameter         | Default   | Explain |
| :---------------- | --------: | :------------------- |
| protocol          | RESP2     | If you use RESP3, you need redis 6.0 environment |
| user              | \<empty\> | Redis server username, requires redis-server 6.0 |
| password          | \<empty\> | Redis server password |
| defaultDatabase   | 0         | Redis server database |
| max poolsize      | 100       | Connection max pool size |
| min poolsize      | 5         | Connection min pool size |
| idleTimeout       | 20000     | Idle time of elements in the connection pool (MS), suitable for connecting to remote redis server |
| connectTimeout    | 10000     | Connection timeout (MS) |
| receiveTimeout    | 10000     | Receive timeout (MS) |
| sendTimeout       | 10000     | Send timeout (MS) |
| encoding          | utf-8     | string charset |
| retry             | 0         | Protocol error retry execution times |
| ssl               | false     | Enable encrypted transmission |
| name              | \<empty\> | Connection name, use client list command to view |
| prefix            | \<empty\> | The prefix of the key, all methods will have this prefix. cli.Set(prefix + "key", 111); |
| exitAutoDisposePool | true | AppDomain.CurrentDomain.ProcessExit/Console.CancelKeyPress auto disposed |
| subscribeReadbytes | false | Subscribe read bytes |

> IPv6: [fe80::b164:55b3:4b4f:7ce6%15]:6379

```csharp
//FreeRedis.DistributedCache
//services.AddSingleton<IDistributedCache>(new FreeRedis.DistributedCache(cli));
```

### 🎣 Master-Slave

```csharp
public static RedisClient cli = new RedisClient(
    "127.0.0.1:6379,password=123,defaultDatabase=13",
    "127.0.0.1:6380,password=123,defaultDatabase=13",
    "127.0.0.1:6381,password=123,defaultDatabase=13"
    );

var value = cli.Get("key1");
```

> Write data at 127.0.0.1:6379; randomly read data from port 6380 or 6381.

### ⛳ Redis Sentinel

```csharp
public static RedisClient cli = new RedisClient(
    "mymaster,password=123", 
    new [] { "192.169.1.10:26379", "192.169.1.11:26379", "192.169.1.12:26379" },
    true //This variable indicates whether to use the read-write separation mode.
    );
```

### 🌌 Redis Cluster

Suppose, a Redis cluster has three master nodes (7001-7003) and three slave nodes (7004-7006), then use the following code to connect to the cluster:

```csharp
public static RedisClient cli = new RedisClient(
    new ConnectionStringBuilder[] { "192.168.0.2:7001", "192.168.0.2:7002", "192.168.0.2:7003" }
    );
```

### ⚡ Client-side-caching

> requires redis-server 6.0 and above

```csharp
cli.UseClientSideCaching(new ClientSideCachingOptions
{
    //Client cache capacity
    Capacity = 3,
    //Filtering rules, which specify which keys can be cached locally
    KeyFilter = key => key.StartsWith("Interceptor"),
    //Check long-term unused cache
    CheckExpired = (key, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromSeconds(2)
});
```

### 📡 Subscribe

```csharp
using (cli.Subscribe("abc", ondata)) //wait .Dispose()
{
    Console.ReadKey();
}

void ondata(string channel, string data) =>
    Console.WriteLine($"{channel} -> {data}");
```

xadd + xreadgroup:

```csharp
using (cli.SubscribeStream("stream_key", ondata)) //wait .Dispose()
{
    Console.ReadKey();
}

void ondata(Dictionary<string, string> streamValue) =>
    Console.WriteLine(JsonConvert.SerializeObject(streamValue));

// NoAck xpending
cli.XPending("stream_key", "FreeRedis__group", "-", "+", 10);
```

lpush + blpop：

```csharp
using (cli.SubscribeList("list_key", ondata)) //wait .Dispose()
{
    Console.ReadKey();
}

void ondata(string listValue) =>
    Console.WriteLine(listValue);
```

### 📃 Scripting

```csharp
var r1 = cli.Eval("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}", 
    new[] { "key1", "key2" }, "first", "second") as object[];

var r2 = cli.Eval("return {1,2,{3,'Hello World!'}}") as object[];

cli.Eval("return redis.call('set',KEYS[1],'bar')", 
    new[] { Guid.NewGuid().ToString() })
```

### 💻 Pipeline

```csharp
using (var pipe = cli.StartPipe())
{
    pipe.IncrBy("key1", 10);
    pipe.Set("key2", Null);
    pipe.Get("key1");

    object[] ret = pipe.EndPipe();
    Console.WriteLine(ret[0] + ", " + ret[2]);
}
```

### 📰 Transaction

```csharp
using (var tran = cli.Multi())
{
    tran.IncrBy("key1", 10);
    tran.Set("key2", Null);
    tran.Get("key1");

    object[] ret = tran.Exec();
    Console.WriteLine(ret[0] + ", " + ret[2]);
}
```

### 📯 GetDatabase: switch database

```csharp
using (var db = cli.GetDatabase(10))
{
    db.Set("key1", 10);
    var val1 = db.Get("key1");
}
```

### 🔍 Scan

> Support cluster mode

```csharp
foreach (var keys in cli.Scan("*", 10, null))
{
    Console.WriteLine(string.Join(", ", keys));
}
```

### 🍡 DelayQueue

```csharp
var delayQueue = cli.DelayQueue("TestDelayQueue");

//Add queue
delayQueue.Enqueue($"Execute in 5 seconds.", TimeSpan.FromSeconds(5));
delayQueue.Enqueue($"Execute in 10 seconds.", DateTime.Now.AddSeconds(10));
delayQueue.Enqueue($"Execute in 15 seconds.", DateTime.Now.AddSeconds(15));
delayQueue.Enqueue($"Execute in 20 seconds.", TimeSpan.FromSeconds(20));
delayQueue.Enqueue($"Execute in 25 seconds.", DateTime.Now.AddSeconds(25));
delayQueue.Enqueue($"Execute in 2024-07-02 14:30:15", DateTime.Parse("2024-07-02 14:30:15"));

//Consumption queue
await delayQueue.DequeueAsync(s =>
{
    output.WriteLine($"{DateTime.Now}：{s}");

    return Task.CompletedTask;
});
```

### 🐆 RediSearch

```csharp
cli.FtCreate(...).Execute();
cli.FtSearch(...).Execute();
cli.FtAggregate(...).Execute();
//... or ...

[FtDocument("index_post", Prefix = "blog:post:")]
class TestDoc
{
    [FtKey]
    public int Id { get; set; }

    [FtTextField("title", Weight = 5.0)]
    public string Title { get; set; }

    [FtTextField("category")]
    public string Category { get; set; }

    [FtTextField("content", Weight = 1.0, NoIndex = true)]
    public string Content { get; set; }

    [FtTagField("tags")]
    public string Tags { get; set; }

    [FtNumericField("views")]
    public int Views { get; set; }
}

var repo = cli.FtDocumentRepository<TestDoc>();
repo.CreateIndex();

repo.Save(new TestDoc { Id = 1, Title = "test title1 word", Category = "class 1", Content = "test content 1 suffix", Tags = "user1,user2", Views = 101 });
repo.Save(new TestDoc { Id = 2, Title = "prefix test title2", Category = "class 2", Content = "test infix content 2", Tags = "user2,user3", Views = 201 });
repo.Save(new TestDoc { Id = 3, Title = "test title3 word", Category = "class 1", Content = "test word content 3", Tags = "user2,user5", Views = 301 });

repo.Delete(1, 2, 3);

repo.Save(new[]
{
    new TestDoc { Id = 1, Title = "test title1 word", Category = "class 1", Content = "test content 1 suffix", Tags = "user1,user2", Views = 101 },
    new TestDoc { Id = 2, Title = "prefix test title2", Category = "class 2", Content = "test infix content 2", Tags = "user2,user3", Views = 201 },
    new TestDoc { Id = 3, Title = "test title3 word", Category = "class 1", Content = "test word content 3", Tags = "user2,user5", Views = 301 }
});

var list = repo.Search("*").InFields(a => new { a.Title }).ToList();
list = repo.Search("*").Return(a => new { a.Title, a.Tags }).ToList();
list = repo.Search("*").Return(a => new { tit1 = a.Title, tgs1 = a.Tags, a.Title, a.Tags }).ToList();

list = repo.Search(a => a.Title == "word").Filter(a => a.Views, 1, 1000).ToList();
list = repo.Search("word").ToList();
list = repo.Search("@title:word").ToList();
```

## 👯 Contributors

<a href="https://github.com/2881099/FreeRedis/graphs/contributors">
  <img src="https://contributors-img.web.app/image?repo=2881099/FreeRedis" />
</a>

## 💕 Donation

> Thank you for your donation

- [Alipay](https://www.cnblogs.com/FreeSql/gallery/image/338860.html)

- [WeChat](https://www.cnblogs.com/FreeSql/gallery/image/338859.html)

## 🗄 License

[MIT](LICENSE)

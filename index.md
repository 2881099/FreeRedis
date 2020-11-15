<h1 align="center"> 🦄 FreeRedis </h1>

<div align="center">

FreeRedis is .NET redis client, supports .NETCore 2.1+, .NETFramework 4.0+, And Xamarin

[![nuget](https://img.shields.io/nuget/v/FreeRedis.svg?style=flat-square)](https://www.nuget.org/packages/FreeRedis) 
[![stats](https://img.shields.io/nuget/dt/FreeRedis.svg?style=flat-square)](https://www.nuget.org/stats/packages/FreeRedis?groupby=Version) 
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://raw.githubusercontent.com/2881099/FreeRedis/master/LICENSE.txt)

</div>

- 🌈 RedisClient Keep all method names consistent with redis-cli
- 🌌 Support Redis Cluster (requires redis-server 3.2 and above)
- ⛳ Support Redis Sentinel
- 🎣 Support Redis Master-Slave
- 📡 Support Redis Pub-Sub
- 📃 Support Redis Lua Scripting
- 💻 Support Pipeline
- 📰 Support Transaction
- 🌴 Support Geo type commands (requires redis-server 3.2 and above)
- 🌲 Support Streams type commands (requires redis-server 5.0 and above)
- ⚡ Support Client-side-cahing (requires redis-server 6.0 and above)
- 🌳 Support Redis 6 RESP3 Protocol

QQ群：4336577(已满)、8578575(在线)、52508226(在线)

#### 🌈 Single machine redis (单机)

```csharp
public static RedisClient cli = new RedisClient("127.0.0.1:6379,password=123,defaultDatabase=13");
//cli.Serialize = obj => JsonConvert.SerializeObject(obj);
//cli.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
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
| ssl               | false     | Enable encrypted transmission |
| name              | \<empty\> | Connection name, use client list command to view |
| prefix            | \<empty\> | key前辍，所有方法都会附带此前辍，cli.Set(prefix + "key", 111); |

> IPv6: [fe80::b164:55b3:4b4f:7ce6%15]:6379

-----

#### 🎣 Master-Slave (读写分离)

```csharp
public static RedisClient cli = new RedisClient(
    "127.0.0.1:6379,password=123,defaultDatabase=13",
    "127.0.0.1:6380,password=123,defaultDatabase=13",
    "127.0.0.1:6381,password=123,defaultDatabase=13"
    );

var value = cli.Get("key1");
```

> 写入时连接 127.0.0.1:6379，读取时随机连接 6380 6381

#### ⛳ Redis Sentinel (哨兵高可用)

```csharp
public static RedisClient cli = new RedisClient(
    "mymaster,password=123", 
    new [] { "192.169.1.10:26379", "192.169.1.11:26379", "192.169.1.12:26379" },
    true //是否读写分离
    );
```

#### 🌌 Redis Cluster (集群)

假如你有一个 Redis Cluster 集群，其中有三个主节点(7001-7003)、三个从节点(7004-7006)，则连接此集群的代码：

```csharp
public static RedisClient cli = new RedisClient(
    new ConnectionStringBuilder[] { "192.168.0.2:7001", "192.168.0.2:7001", "192.168.0.2:7003" }
    );
```

-----

#### ⚡ Client-side-cahing (本地缓存)

> requires redis-server 6.0 and above

```csharp
cli.UseClientSideCaching(new ClientSideCachingOptions
{
    //本地缓存的容量
    Capacity = 3,
    //过滤哪些键能被本地缓存
    KeyFilter = key => key.StartsWith("Interceptor"),
    //检查长期未使用的缓存
    CheckExpired = (key, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromSeconds(2)
});
```

#### 📡 Subscribe (订阅)

```csharp
using (cli.Subscribe("abc", ondata)) //wait .Dispose()
{
    Console.ReadKey();
}

void ondata(string channel, string data) =>
    Console.WriteLine($"{channel} -> {data}");
```

#### 📃 Scripting (脚本)

```csharp
var r1 = cli.Eval("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}", 
    new[] { "key1", "key2" }, "first", "second") as object[];

var r2 = cli.Eval("return {1,2,{3,'Hello World!'}}") as object[];

cli.Eval("return redis.call('set',KEYS[1],'bar')", 
    new[] { Guid.NewGuid().ToString() })
```

#### 💻 Pipeline (管道)

```csharp
using (var pipe = cli.StartPipe())
{
    pipe.IncrBy("key1", 10);
    pipe.Set("key2", Null);
    pipe.Get("key1");

    object[] ret = pipe.EndPipe();
    Console.WriteLine(ret[0] + ", " + ret[2]);
}

// or Async Callback

using (var pipe = cli.StartPipe())
{
    var tasks = new List<Task>();
    long t0 = 0;
    task.Add(pipe.IncrByAsync("key1", 10).ContinueWith(t => t0 = t.Result)); //callback

    pipe.SetAsync("key2", Null);

    string t2 = null;
    task.Add(pipe.GetAsync("key1").ContinueWith(t => t2 = t.Result)); //callback

    pipe.EndPipe();
    Task.WaitAll(tasks.ToArray()); //wait all callback
    Console.WriteLine(t0 + ", " + t2);
}
```

#### 📰 Transaction (事务)

```csharp
using (var tran = cli.Multi())
{
    tran.IncrBy("key1", 10);
    tran.Set("key2", Null);
    tran.Get("key1");

    object[] ret = tran.Exec();
    Console.WriteLine(ret[0] + ", " + ret[2]);
}

// or Async Callback

using (var tran = cli.Multi())
{
    var tasks = new List<Task>();
    long t0 = 0;
    task.Add(tran.IncrByAsync("key1", 10).ContinueWith(t => t0 = t.Result)); //callback

    tran.SetAsync("key2", Null);

    string t2 = null;
    task.Add(tran.GetAsync("key1").ContinueWith(t => t2 = t.Result)); //callback

    tran.Exec();
    Task.WaitAll(tasks.ToArray()); //wait all callback
    Console.WriteLine(t0 + ", " + t2);
}
```

#### 📯 GetDatabase (切库)

```csharp
using (var db = cli.GetDatabase(10))
{
    db.Set("key1", 10);
    var val1 = db.Get("key1");
}
```

#### 💕 Donation (捐赠)

> Thank you for your donation

- [Alipay](https://www.cnblogs.com/FreeSql/gallery/image/338860.html)

- [WeChat](https://www.cnblogs.com/FreeSql/gallery/image/338859.html)

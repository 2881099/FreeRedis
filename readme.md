## 🦄 　FreeRedis

[![nuget](https://img.shields.io/nuget/v/FreeSql.svg?style=flat-square)](https://www.nuget.org/packages/FreeSql) [![stats](https://img.shields.io/nuget/dt/FreeSql.svg?style=flat-square)](https://www.nuget.org/stats/packages/FreeSql?groupby=Version) [![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/2881099/FreeSql/master/LICENSE.txt)

FreeRedis 是功能强大的 redis 客户端组件，支持 .NETCore 2.1+ 或 .NETFramework 4.0+ 或 Xamarin。

- RedisClient Keep all method names consistent with redis-cli
- Support geo type commands (redis-server 3.2 or above is required)
- Support Redis Cluster
- Support Redis Sentinel
- Support Redis Master-Slave
- Supports stream type commands (requires redis-server 5.0 and above)
- Supports Redis 6 RESP3 Protocol

QQ群：4336577(已满)、8578575(在线)、52508226(在线)

#### Single machine redis (单机)

```csharp
public static RedisClient cli = new RedisClient("127.0.0.1:6379,password=123,defaultDatabase=13");

var value = cli.Get("key1");
```

| Parameter         | Default   | Explain |
| :---------------- | --------: | :------------------- |
| Protocol          | RESP2     | If you use RESP3, you need redis 6.0 environment | 
| user              | \<Empty\> | Redis server username, requires redis-server 6.0 |
| password          | \<Empty\> | Redis server password |
| defaultDatabase   | 0         | Redis server database |
| max pool size     | 100       | Connection max pool size |
| min pool size     | 5         | Connection min pool size |
| idleTimeout       | 20000     | Idle time of elements in the connection pool (MS), suitable for connecting to remote redis server |
| connectTimeout    | 5000      | Connection timeout (MS) |
| receiveTimeout    | 10000     | Receive timeout (MS) |
| sendTimeout       | 10000     | Send timeout (MS) |
| encoding          | utf-8     | string charset |
| ssl               | false     | Enable encrypted transmission |
| name              | \<Empty\> | Connection name, use client list command to view |
| prefix            | \<Empty\> | key前辍，所有方法都会附带此前辍，cli.Set(prefix + "key", 111); |

> IPv6: [fe80::b164:55b3:4b4f:7ce6%15]:6379

#### Master-Slave (读写分离)

```csharp
public static cli = new RedisClient(
    "127.0.0.1:6379,password=123,defaultDatabase=13",
    "127.0.0.1:6380,password=123,defaultDatabase=13",
    "127.0.0.1:6381,password=123,defaultDatabase=13");

var value = cli.Get("key1");
```

> 写入连接 127.0.0.1:6379，读取随机连接 6380 6381

#### Redis Sentinel (哨兵高可用)

```csharp
public static cli = new RedisClient(
    "mymaster,password=123", 
    new [] { "192.169.1.10:26379", "192.169.1.11:26379", "192.169.1.12:26379" },
    true //是否读写分离
    );
```

#### Redis Cluster (集群)

待完成...

#### Pipeline (管道)

```csharp
using (var pipe = cli.StartPipe())
{
    pipe.IncrBy("key1", 10);
    pipe.Set("key2", Null);
    pipe.Get("key1");
    object[] ret = pipe.EndPipe();
}
```

#### Transaction (事务)

```csharp
using (var tran = cli.Multi())
{
    tran.IncrBy("key1", 10);
    tran.Set("key2", Null);
    tran.Get("key1");
    object[] ret = tran.Exec();
}
```

#### 💕 　Donation

> Thank you for your donation

- [Alipay](https://www.cnblogs.com/FreeSql/gallery/image/338860.html)

- [WeChat](https://www.cnblogs.com/FreeSql/gallery/image/338859.html)

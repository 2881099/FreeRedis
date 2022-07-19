using FreeRedis;
using System.Diagnostics;

class Program
{
    static RedisClient CreateRedisClient()
    {
        var cli = new RedisClient(new[] { (ConnectionStringBuilder)"192.168.164.10:6380,password=123456" });
        cli.UseClientSideCaching(new ClientSideCachingOptions
        {
            //本地缓存的容量
            Capacity = 99999,
            //过滤哪些键能被本地缓存
            KeyFilter = key => true,
            //检查长期未使用的缓存
            CheckExpired = (key, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromSeconds(600)
        });
        return cli;
    }


    static void Main(string[] args)
    {

        var testCount = 50;
        Console.WriteLine($"测试 {testCount} 个 RedisClient Cluster 客户端缓存功能 client side cahcing");
        Console.WriteLine($"正在创建 {testCount} 个 RedisCient 对象...\r\n");
        var clis = new List<RedisClient>();
        for(var a = 0; a < testCount; a++)
        {
            clis.Add(CreateRedisClient());
        }

        var key = Guid.NewGuid().ToString();
        string value = null;

        Console.WriteLine($"{clis.Count} 个 RedisClient 对象创建完成.\r\n");
        Console.WriteLine($"【Esc】退出程序");
        Console.WriteLine($"【Enter】对比 {clis.Count} 值同步");
        Console.WriteLine($"【1】刷新新值\r\n");

        Console.WriteLine($"本次测试 key: {key}\r\n\r\n");

        while (true)
        {
            var readkey = Console.ReadKey().Key;
            if (readkey == ConsoleKey.Escape) break;

            if (readkey == ConsoleKey.Enter)
            {
                var sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                var equalsCounter = 0;
                foreach (var cli in clis)
                {
                    if (value == cli.Get(key)) equalsCounter++;
                }
                sw.Stop();
                Console.WriteLine($"RedisClient[0..{clis.Count}] Get 新值比较，耗时 {sw.ElapsedMilliseconds}ms，相同 {equalsCounter}，不相同 {(clis.Count - equalsCounter)}");
            }

            if (readkey == ConsoleKey.D1)
            {
                value = Guid.NewGuid().ToString();
                clis[0].Set(key, value);
                Console.WriteLine($"RedisClient[0] Set 新值已写入 {value}");
            }
        }

        Console.WriteLine($"正在退出...");
        foreach (var cli in clis) cli.Dispose();
        Console.WriteLine($"退出成功.");
    }
}
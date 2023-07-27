using FreeRedis.NewClient.Client;

namespace ConsoleApp2
{
    internal class Program
    {

        private static readonly FreeRedisClient _freeRedis;
        static Program()
        {
            _freeRedis = new("192.168.1.79:9379,password=0f649985edfdf11ae10a,defaultDatabase=0", msg => Console.WriteLine(msg));
        }


        static void Main(string[] args)
        {

            Test("0f649985edfdf11ae10a");
            Test("1");
            Test("123456");
            Test("1");
            Test("0f649985edfdf11ae10a");
            for (int i = 0; i < 1000; i++)
            {
                //TaskNumber();
            }
            Console.ReadKey();

        }

        /*
        public async static void TaskNumber()
        {
            if (await _freeRedis.SetAsync("K1", "V1"))
            {
                if (await _freeRedis.ExistAsync("K1") ==1)
                {
                    Console.WriteLine("SET Succeed!");
                }
                
            }
            if (await _freeRedis.SetObjAsync("K2", new { name = "xiaoming", age = 10 }))
            {
                if (await _freeRedis.ExistAsync("K2") == 1)
                {
                    Console.WriteLine("SET Succeed!");
                }
            }
        }
        */
        public async static void Test(string password)
        {
            try
            {
                if (await _freeRedis.AuthAsync(password))
                {
                    Console.WriteLine($"Password Succeed!");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
    }
}
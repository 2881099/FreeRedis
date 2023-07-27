namespace ConsoleApp1
{
    internal class Program
    {
        private static int a = 0;
        static void Main(string[] args)
        {
            CircleBuffer<int> circleBuffer = new CircleBuffer<int>(0,5);

            var resut = Parallel.For(0, 1000, i => { 
            
                circleBuffer.ConcurrentEnqueue(i);
                
            });


            while (!resut.IsCompleted)
            {

            }
            circleBuffer.Show();


            Parallel.For(0, 500, i =>
            {

               circleBuffer.ConcurrentDequeue();

            });
            var result = 0;
            Parallel.For(0, 500, i =>
            {

                Interlocked.Add(ref result, circleBuffer.ConcurrentDequeue());

            });
            Console.WriteLine(result);
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                for (int i = 0; i < 7; i++)
                {
                    circleBuffer.Dequeue();
                }
            });
            /*
            for (int i = 0; i < 15; i++)
            {
                circleBuffer.Enqueue(i);
                if (i<7)
                {
                    circleBuffer.Dequeue();
                }
            }

            Console.WriteLine("Completed!");
            Console.ReadKey();

            Task.Run(() =>
            {
                Thread.Sleep(1000);
                for (int i = 0; i < 5; i++)
                {
                    circleBuffer.Dequeue();
                }
            });

            for (int i = 0; i < 5; i++)
            {
                circleBuffer.Enqueue(i);
            }
            //ParallelOptions parallelOptions = new ParallelOptions();
            //parallelOptions.MaxDegreeOfParallelism = 1;
            //Parallel.For(0, 30, parallelOptions, async (i) =>
            //{
            //    Thread.Sleep(1000);
            //    await Show();
            //});
            */
            Console.ReadKey();
        }

        public static async Task Show()
        {
            await Task.Delay(1000);
            Console.WriteLine("Hello, World!");
        }
    }

}
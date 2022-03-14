using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class ClientPool5
    {

        private readonly static Func<Task<bool>, bool, bool> _setResult;
        protected readonly static Func<object, TaskCreationOptions, Task<bool>> CreateTask;
        protected readonly static Func<Task<bool>> CreateTaskWithoutParameters;
        static ClientPool5()
        {
            _setResult = typeof(Task<bool>)
                .GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(bool) }, null)
                .CreateDelegate<Func<Task<bool>, bool, bool>>();


            var ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(TaskCreationOptions) }, null);

            DynamicMethod dynamicMethod = new DynamicMethod("GETTASK1", typeof(Task<bool>), new Type[] { typeof(object), typeof(TaskCreationOptions) });
            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            CreateTask = (Func<object, TaskCreationOptions, Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<object, TaskCreationOptions, Task<bool>>));
            ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);
            dynamicMethod = new DynamicMethod("GETTASK2", typeof(Task<bool>), new Type[0]);
            iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            CreateTaskWithoutParameters = (Func<Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<Task<bool>>));
            //CreateTask = () => (new TaskCompletionSource<bool>(null, TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }


        private string _ip;
        private int _port;
        private NewRedisClient26 _node1;
        private NewRedisClient26 _node2;
        private NewRedisClient26 _node3;
        private const int _length = 2;
        public int[] CallCounter;
        public ClientPool5(string ip, int port)
        {
            _ip = ip;
            _port = port;

            _node1 = new NewRedisClient26();
            _node1.CreateConnection(ip, port);

            _node2 = new NewRedisClient26();
            _node2.CreateConnection(ip, port);

            _node3 = new NewRedisClient26();
            _node3.CreateConnection(ip, port);

        }

        private int count = 0;
        Stopwatch stopwatch = new Stopwatch();


        public Task<bool> AuthAsync(string password)
        {
            _node1.AuthAsync(password);
            _node2.AuthAsync(password);
            return _node3.AuthAsync(password);
        }
        public void Start()
        {

            //stopwatch.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsync(byte[] bytes)
        {
            if (_node1.TryGetSendLock())
            {
                return _node1.SetAsyncWithoutLock(bytes);
            }
            else if (_node2.TryGetSendLock())
            {
                return _node2.SetAsyncWithoutLock(bytes);
            }
            else
            {
                return _node3.SetAsync(bytes);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsync(string key, string value)
        {

            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            return SetAsync(bytes);

        }

        public void ShowHandlerCount()
        {
            //Console.WriteLine("HandlerCount1:" + _node1.HandlerCount);
            //Console.WriteLine("HandlerCount2:" + _node2.HandlerCount);
            //Console.WriteLine("HandlerCount3:" + _node3.HandlerCount);
        }
        public int GetLockCount1()
        {
            return _node1.LockCount;
        }
        public int GetLockCount2()
        {
            return _node2.LockCount;
        }
        public int GetLockCount3()
        {
            return _node3.LockCount;
        }
    }
}

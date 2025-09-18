using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis.Internal.ObjectPool
{
    internal class TestTrace
    {
        internal static void WriteLine(string text, ConsoleColor backgroundColor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(text);
            }
            catch { }
            return;
            //try //#643
            //{
            //    var bgcolor = Console.BackgroundColor;
            //    var forecolor = Console.ForegroundColor;
            //    Console.BackgroundColor = backgroundColor;

            //    switch (backgroundColor)
            //    {
            //        case ConsoleColor.Yellow:
            //            Console.ForegroundColor = ConsoleColor.White;
            //            break;
            //        case ConsoleColor.DarkGreen:
            //            Console.ForegroundColor = ConsoleColor.White;
            //            break;
            //    }
            //    Console.Write(text);
            //    Console.BackgroundColor = bgcolor;
            //    Console.ForegroundColor = forecolor;
            //    Console.WriteLine();
            //}
            //catch
            //{
            //    try
            //    {
            //        System.Diagnostics.Debug.WriteLine(text);
            //    }
            //    catch { }
            //}
        }
    }

    /// <summary>
    /// 对象池管理类
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public partial class ObjectPool<T> : IObjectPool<T>
    {
        private enum State { Healthy, Observing, Unavailable }

        public IPolicy<T> Policy { get; protected set; }

        private object _allObjectsLock = new object();
        internal List<Object<T>> _allObjects = new List<Object<T>>();
        internal ConcurrentStack<Object<T>> _freeObjects = new ConcurrentStack<Object<T>>();

        private ConcurrentQueue<GetSyncQueueInfo> _getSyncQueue = new ConcurrentQueue<GetSyncQueueInfo>();
        private ConcurrentQueue<TaskCompletionSource<Object<T>>> _getAsyncQueue = new ConcurrentQueue<TaskCompletionSource<Object<T>>>();
        private ConcurrentQueue<bool> _getQueue = new ConcurrentQueue<bool>();

        private volatile State _currentState = State.Healthy;
        private Exception _firstFailureException;
        private readonly object _stateTransitionLock = new object();
        private bool _running = true;
        private bool _isCheckerRunning = false;

        public bool IsAvailable => this.UnavailableException == null;
        public Exception UnavailableException { get; private set; }
        public DateTime? UnavailableTime { get; private set; }
        public DateTime? AvailableTime { get; private set; }

        /// <summary>
        /// 报告一次失败。这是新的故障处理入口。
        /// 它会根据策略决定是进入“观察期”还是直接“熔断”。
        /// </summary>
        public bool SetUnavailable(Exception exception, DateTime lastGetTime)
        {
            // 如果策略未开启容忍窗口，则使用旧的直接熔断逻辑
            if (Policy.ToleranceWindow <= TimeSpan.Zero)
            {
                lock (_stateTransitionLock)
                {
                    _isCheckerRunning = false;
                    return TransitionToUnavailable(exception, lastGetTime, true);
                }
            }

            // --- 使用容忍窗口的逻辑 ---
            lock (_stateTransitionLock)
            {
                // 如果当前不处于健康状态，则什么都不做。
                // 这意味着恢复检查器已经在工作了。
                if (_currentState != State.Healthy)
                {
                    return false;
                }

                // 状态从“健康”切换到“观察中”
                _currentState = State.Observing;
                _firstFailureException = exception;

                TestTrace.WriteLine($"[{Policy.Name}] Service unstable: {exception.Message}. Entering a {Policy.ToleranceWindow.TotalSeconds} second observation period.", ConsoleColor.DarkYellow);

                if (!_isCheckerRunning)
                {
                    _isCheckerRunning = true;
                    // 在后台启动高频的恢复检查
                    new Thread(UnifiedCheckerLoop) { IsBackground = true }.Start();
                }

                return true; // 状态发生变化
            }
        }

        /// <summary>
        /// 一个统一的、能处理两种状态（Observing 和 Unavailable）的检查循环。
        /// 这个线程一旦启动，就会一直运行，直到状态恢复为 Healthy 或池被 Dispose。
        /// </summary>
        private void UnifiedCheckerLoop()
        {
            try
            {
                // === 阶段一：观察期的高频检查 ===
                if (_currentState == State.Observing)
                {
                    TestTrace.WriteLine($"[{Policy.Name}] Observation period checker started.", ConsoleColor.DarkGray);
                    var observationEndTime = DateTime.Now.Add(Policy.ToleranceWindow);

                    while (DateTime.Now < observationEndTime)
                    {
                        if (_running == false || _currentState != State.Observing) return;

                        if (TryHealthCheck())
                        {
                            TestTrace.WriteLine($"[{Policy.Name}] Service recovery detected during the observation period.", ConsoleColor.DarkGreen);
                            RestoreToAvailable();
                            return; // 成功，退出线程
                        }

                        Thread.Sleep(Policy.ToleranceCheckInterval);
                    }

                    // 观察期结束仍未恢复，转换到熔断状态
                    lock (_stateTransitionLock)
                    {
                        if (_currentState == State.Observing)
                        {
                            TestTrace.WriteLine($"[{Policy.Name}] Observation period ended, service not recovered. Now breaking the circuit.", ConsoleColor.Red);
                            // 注意：这里不再启动新线程，而是让当前线程继续工作
                            TransitionToUnavailable(_firstFailureException, DateTime.Now, false);
                        }
                    }
                }

                // === 阶段二：熔断后的低频检查 ===
                if (_currentState == State.Unavailable)
                {
                    TestTrace.WriteLine($"[{Policy.Name}] Low-frequency recovery checker has taken over.", ConsoleColor.DarkGray);
                    while (_currentState == State.Unavailable)
                    {
                        if (_running == false) return;

                        // 使用低频间隔
                        Thread.Sleep(Policy.AvailableCheckInterval);

                        if (_running == false) return;
                        if (_currentState != State.Unavailable) return; // 可能在休眠期间状态已改变

                        if (TryHealthCheck())
                        {
                            RestoreToAvailable();
                            return; // 成功，退出线程
                        }
                        else
                        {
                            TestTrace.WriteLine($"[{Policy.Name}] Recovery check failed. Next check at: {DateTime.Now.Add(Policy.AvailableCheckInterval)}", ConsoleColor.DarkYellow);
                        }
                    }
                }
            }
            finally
            {
                // --- 线程结束时，重置标志 ---
                // 无论线程是正常退出（恢复成功）还是异常退出，
                // 都必须确保重置标志，以便下次可以启动新的检查器。
                lock (_stateTransitionLock)
                {
                    _isCheckerRunning = false;
                }
                TestTrace.WriteLine($"[{Policy.Name}] Checker thread stopped.", ConsoleColor.DarkGray);
            }
        }

        /// <summary>
        /// 将连接池状态切换为“不可用”
        /// </summary>
        private bool TransitionToUnavailable(Exception exception, DateTime lastGetTime, bool isAvailableCheck)
        {
            if (_currentState == State.Unavailable) return false;
            if (AvailableTime != null && lastGetTime < AvailableTime) return false;

            UnavailableException = exception;
            UnavailableTime = DateTime.Now;
            AvailableTime = null;
            _currentState = State.Unavailable;

            Policy.OnUnavailable();
            if (isAvailableCheck)
            {
                if (!_isCheckerRunning)
                {
                    _isCheckerRunning = true;
                    CheckUntilAvailable(Policy.AvailableCheckInterval); // 启动“低频”的后台恢复检查
                }
            }
            return true;
        }

        /// <summary>
        /// 后台定时检查可用性（原 CheckAvailable 方法）
        /// </summary>
        private void CheckUntilAvailable(TimeSpan interval)
        {
            new Thread(() =>
            {
                if (_currentState == State.Unavailable)
                    TestTrace.WriteLine($"[{Policy.Name}] Service is circuit-broken. Next recovery check at: {DateTime.Now.Add(interval)}", ConsoleColor.DarkYellow);

                while (_currentState == State.Unavailable)
                {
                    if (_running == false) return;
                    Thread.Sleep(interval);
                    if (_running == false) return;

                    if (TryHealthCheck())
                    {
                        RestoreToAvailable();
                        break; // 恢复成功，退出循环
                    }
                    else
                    {
                        TestTrace.WriteLine($"[{Policy.Name}] Recovery check failed. Next check at: {DateTime.Now.Add(interval)}", ConsoleColor.DarkYellow);
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        /// <summary>
        /// 尝试进行一次健康检查的通用逻辑
        /// </summary>
        /// <returns>检查是否成功</returns>
        private bool TryHealthCheck()
        {
            Object<T> conn = null;
            try
            {
                conn = GetFree(false); // false 表示不检查池状态，强行获取一个对象
                if (conn == null) throw new Exception($"[{Policy.Name}] Unable to get a resource for health check {this.Statistics}");

                try
                {
                    if (Policy.OnCheckAvailable(conn)) return true;
                    // 如果 OnCheckAvailable 返回 false，我们认为检查失败
                    throw new Exception($"[{Policy.Name}] OnCheckAvailable returned false.");
                }
                catch
                {
                    // 如果检查失败，尝试重置对象，为下一次检查做准备
                    conn.ResetValue();
                    // 再次检查
                    return Policy.OnCheckAvailable(conn);
                }
            }
            catch
            {
                // 任何异常都表示检查失败
                return false;
            }
            finally
            {
                if (conn != null)
                {
                    Return(conn);
                }
            }
        }

        /// <summary>
        /// 将连接池恢复到可用状态
        /// </summary>
        private void RestoreToAvailable()
        {
            bool isRestored = false;
            if (_currentState != State.Healthy)
            {
                lock (_stateTransitionLock)
                {
                    if (_currentState != State.Healthy)
                    {
                        lock (_allObjectsLock)
                            _allObjects.ForEach(a => a.LastGetTime = a.LastReturnTime = new DateTime(2000, 1, 1));

                        UnavailableException = null;
                        UnavailableTime = null;
                        _firstFailureException = null; // 清理首次失败的异常
                        AvailableTime = DateTime.Now;
                        _currentState = State.Healthy; // 状态恢复为健康
                        isRestored = true;
                    }
                }
            }

            if (isRestored)
            {
                Policy.OnAvailable();
                TestTrace.WriteLine($"[{Policy.Name}] Service is now available.", ConsoleColor.DarkGreen);
            }
        }

        protected bool LiveCheckAvailable()
        {
            try
            {
                var conn = GetFree(false);
                if (conn == null) throw new Exception($"[{Policy.Name}] Failed to get resource {this.Statistics}");

                try
                {
                    if (Policy.OnCheckAvailable(conn) == false) throw new Exception($"[{Policy.Name}] An exception needs to be thrown");
                }
                finally
                {
                    Return(conn);
                }
            }
            catch
            {
                return false;
            }

            RestoreToAvailable();
            return true;
        }

        public string Statistics => $"Pool: {_freeObjects.Count}/{_allObjects.Count}, Get wait: {_getSyncQueue.Count}, GetAsync wait: {_getAsyncQueue.Count}, State: {_currentState}";
        public string StatisticsFullily
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(Statistics);
                if (_currentState == State.Observing) sb.AppendLine($"Observing since: {_firstFailureException?.Message}");
                if (_currentState == State.Unavailable) sb.AppendLine($"Unavailable since: {UnavailableException?.Message}");
                sb.AppendLine("");

                foreach (var obj in _allObjects)
                {
                    sb.AppendLine($"{obj.Value}, Times: {obj.GetTimes}, ThreadId(R/G): {obj.LastReturnThreadId}/{obj.LastGetThreadId}, Time(R/G): {obj.LastReturnTime:yyyy-MM-dd HH:mm:ss:ms}/{obj.LastGetTime:yyyy-MM-dd HH:mm:ss:ms}, ");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="poolsize">池大小</param>
        /// <param name="createObject">池内对象的创建委托</param>
        /// <param name="onGetObject">获取池内对象成功后，进行使用前操作</param>
        public ObjectPool(int poolsize, Func<T> createObject, Action<Object<T>> onGetObject = null) : this(new DefaultPolicy<T> { PoolSize = poolsize, CreateObject = createObject, OnGetObject = onGetObject })
        {
        }
        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="policy">策略</param>
        public ObjectPool(IPolicy<T> policy)
        {
            Policy = policy;

            AppDomain.CurrentDomain.ProcessExit += (s1, e1) =>
            {
                if (Policy.IsAutoDisposeWithSystem)
                    _running = false;
            };
            try
            {
                Console.CancelKeyPress += (s1, e1) =>
                {
                    if (e1.Cancel) return;
                    if (Policy.IsAutoDisposeWithSystem)
                        _running = false;
                };
            }
            catch { }
        }

        public void AutoFree()
        {
            if (_running == false) return;
            if (UnavailableException != null) return;

            var list = new List<Object<T>>();
            while (_freeObjects.TryPop(out var obj))
                list.Add(obj);
            foreach (var obj in list)
            {
                if (obj != null && obj.Value == null ||
                    obj != null && Policy.IdleTimeout > TimeSpan.Zero && DateTime.Now.Subtract(obj.LastReturnTime) > Policy.IdleTimeout)
                {
                    if (obj.Value != null)
                    {
                        Return(obj, true);
                        continue;
                    }
                }
                Return(obj);
            }
        }

        /// <summary>
        /// 获取可用资源，或创建资源
        /// </summary>
        private Object<T> GetFree(bool checkAvailable)
        {
            if (_running == false)
                throw new ObjectDisposedException($"[{Policy.Name}] The ObjectPool has been disposed, see: https://github.com/dotnetcore/FreeSql/discussions/1079");

            if (checkAvailable)
            {
                var currentState = _currentState;
                if (currentState == State.Unavailable)
                    throw new Exception($"[{Policy.Name}] The service is circuit-broken, waiting for recovery. Error: {UnavailableException?.Message}", UnavailableException);

                //if (currentState == State.Observing)
                //    throw new Exception($"[{Policy.Name}] The service is unstable, checking for recovery. Original error: {_firstFailureException?.Message}", _firstFailureException);
            }

            if ((_freeObjects.TryPop(out var obj) == false || obj == null) && _allObjects.Count < Policy.PoolSize)
            {
                lock (_allObjectsLock)
                    if (_allObjects.Count < Policy.PoolSize)
                        _allObjects.Add(obj = new Object<T> { Pool = this, Id = _allObjects.Count + 1 });
            }

            if (obj != null)
                obj._isReturned = false;

            if (obj != null && obj.Value == null ||
                obj != null && Policy.IdleTimeout > TimeSpan.Zero && DateTime.Now.Subtract(obj.LastReturnTime) > Policy.IdleTimeout)
            {
                try
                {
                    obj.ResetValue();
                }
                catch
                {
                    Return(obj);
                    throw;
                }
            }

            return obj;
        }

        public Object<T> Get(TimeSpan? timeout = null)
        {
            var obj = GetFree(true);
            if (obj == null)
            {
                var queueItem = new GetSyncQueueInfo();

                _getSyncQueue.Enqueue(queueItem);
                _getQueue.Enqueue(false);

                if (timeout == null) timeout = Policy.SyncGetTimeout;

                try
                {
                    if (queueItem.Wait.Wait(timeout.Value))
                        obj = queueItem.ReturnValue;
                }
                catch { }

                if (obj == null) obj = queueItem.ReturnValue;
                if (obj == null) lock (queueItem.Lock) queueItem.IsTimeout = (obj = queueItem.ReturnValue) == null;
                if (obj == null) obj = queueItem.ReturnValue;
                if (queueItem.Exception != null) throw queueItem.Exception;

                if (obj == null)
                {
                    Policy.OnGetTimeout();
                    if (Policy.IsThrowGetTimeoutException)
                        throw new TimeoutException($"[{Policy.Name}] ObjectPool.Get() timeout {timeout.Value.TotalSeconds} seconds, see: https://github.com/dotnetcore/FreeSql/discussions/1081");

                    return null;
                }
            }

            try
            {
                Policy.OnGet(obj);
            }
            catch
            {
                Return(obj, true);
                throw;
            }

            obj.LastGetThreadId = Thread.CurrentThread.ManagedThreadId;
            obj.LastGetTime = DateTime.Now;
            obj.LastGetTimeCopy = DateTime.Now;
            Interlocked.Increment(ref obj._getTimes);

            return obj;
        }

#if net40
#else
        async public Task<Object<T>> GetAsync()
        {
            var obj = GetFree(true);
            if (obj == null)
            {
                if (Policy.AsyncGetCapacity > 0 && _getAsyncQueue.Count >= Policy.AsyncGetCapacity - 1)
                    throw new OutOfMemoryException($"[{Policy.Name}] ObjectPool.GetAsync() The queue is too long. Policy.AsyncGetCapacity = {Policy.AsyncGetCapacity}");

                var tcs = new TaskCompletionSource<Object<T>>();

                _getAsyncQueue.Enqueue(tcs);
                _getQueue.Enqueue(true);

                obj = await tcs.Task;

                //if (timeout == null) timeout = Policy.SyncGetTimeout;

                //if (tcs.Task.Wait(timeout.Value))
                //	obj = tcs.Task.Result;

                //if (obj == null) {

                //	tcs.TrySetCanceled();
                //	Policy.GetTimeout();

                //	if (Policy.IsThrowGetTimeoutException)
                //		throw new TimeoutException($"[{Policy.Name}] ObjectPool.GetAsync() timeout {timeout.Value.TotalSeconds} seconds, see: https://github.com/dotnetcore/FreeSql/discussions/1081");

                //	return null;
                //}
            }

            try
            {
                await Policy.OnGetAsync(obj);
            }
            catch
            {
                Return(obj, true);
                throw;
            }

            obj.LastGetThreadId = Thread.CurrentThread.ManagedThreadId;
            obj.LastGetTime = DateTime.Now;
            obj.LastGetTimeCopy = DateTime.Now;
            Interlocked.Increment(ref obj._getTimes);

            return obj;
        }
#endif

        public void Return(Object<T> obj, bool isReset = false)
        {
            if (obj == null) return;
            if (obj._isReturned) return;

            if (_running == false)
            {
                Policy.OnDestroy(obj.Value);
                try { (obj.Value as IDisposable)?.Dispose(); } catch { }
                return;
            }

            if (isReset) obj.ResetValue();
            bool isReturn = false;

            while (isReturn == false && _getQueue.TryDequeue(out var isAsync))
            {
                if (isAsync == false)
                {
                    if (_getSyncQueue.TryDequeue(out var queueItem) && queueItem != null)
                    {
                        lock (queueItem.Lock)
                            if (queueItem.IsTimeout == false)
                                queueItem.ReturnValue = obj;

                        if (queueItem.ReturnValue != null)
                        {
                            if (UnavailableException != null)
                            {
                                queueItem.Exception = new Exception($"[{Policy.Name}] Status unavailable, waiting for recovery. {UnavailableException?.Message}", UnavailableException);
                                try
                                {
                                    queueItem.Wait.Set();
                                }
                                catch { }
                            }
                            else
                            {
                                obj.LastReturnThreadId = Thread.CurrentThread.ManagedThreadId;
                                obj.LastReturnTime = DateTime.Now;

                                try
                                {
                                    queueItem.Wait.Set();
                                    isReturn = true;
                                }
                                catch { }
                            }
                        }

                        try { queueItem.Dispose(); } catch { }
                    }
                }
                else
                {
                    if (_getAsyncQueue.TryDequeue(out var tcs) && tcs != null && tcs.Task.IsCanceled == false)
                    {
                        if (UnavailableException != null)
                        {
                            try
                            {
                                tcs.TrySetException(new Exception($"[{Policy.Name}] Status unavailable, waiting for recovery. {UnavailableException?.Message}", UnavailableException));
                            }
                            catch { }
                        }
                        else
                        {
                            obj.LastReturnThreadId = Thread.CurrentThread.ManagedThreadId;
                            obj.LastReturnTime = DateTime.Now;

                            try
                            {
                                isReturn = tcs.TrySetResult(obj);
                            }
                            catch { }
                        }
                    }
                }
            }

            //无排队，直接归还
            if (isReturn == false)
            {
                try
                {
                    Policy.OnReturn(obj);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    obj.LastReturnThreadId = Thread.CurrentThread.ManagedThreadId;
                    obj.LastReturnTime = DateTime.Now;
                    obj._isReturned = true;

                    _freeObjects.Push(obj);
                }
            }
        }

        public void Dispose()
        {
            _running = false;

            while (_freeObjects.TryPop(out var fo)) ;
            while (_getSyncQueue.TryDequeue(out var sync))
            {
                try { sync.Wait.Set(); } catch { }
            }

            while (_getAsyncQueue.TryDequeue(out var async))
                async.TrySetCanceled();

            while (_getQueue.TryDequeue(out var qs)) ;

            for (var a = 0; a < _allObjects.Count; a++)
            {
                Policy.OnDestroy(_allObjects[a].Value);
                try { (_allObjects[a].Value as IDisposable)?.Dispose(); } catch { }
            }

            _allObjects.Clear();
        }

        class GetSyncQueueInfo : IDisposable
        {
            internal ManualResetEventSlim Wait { get; set; } = new ManualResetEventSlim();
            internal Object<T> ReturnValue { get; set; }
            internal object Lock = new object();
            internal bool IsTimeout { get; set; } = false;
            internal Exception Exception { get; set; }

            public void Dispose()
            {
                try
                {
                    if (Wait != null)
                        Wait.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
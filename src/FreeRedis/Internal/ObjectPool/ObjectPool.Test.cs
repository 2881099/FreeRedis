// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Threading;
// using System.Threading.Tasks;

// namespace FreeRedis.Internal.ObjectPool
// {
//     public static class ObjectPoolTest
//     {
//         sealed class StressResource : IDisposable
//         {
//             public int ResourceId { get; set; }
//             public void Dispose() { }
//             public override string ToString() => $"ResourceId: {ResourceId}";
//         }

//         class StressPolicy : IPolicy<StressResource>
//         {
//             int _resourceSeed;

//             public string Name { get; set; } = nameof(ObjectPoolTest);
//             public int PoolSize { get; set; } = 8;
//             public TimeSpan SyncGetTimeout { get; set; } = TimeSpan.FromSeconds(5);
//             public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMilliseconds(120);
//             public TimeSpan IdleCheckTimeout { get; set; } = TimeSpan.FromMilliseconds(30);
//             public int AsyncGetCapacity { get; set; } = 10000;
//             public bool IsThrowGetTimeoutException { get; set; } = true;
//             public bool IsAutoDisposeWithSystem { get; set; } = false;
//             public TimeSpan AvailableCheckInterval { get; set; } = TimeSpan.FromSeconds(1);
//             public TimeSpan ToleranceWindow { get; set; } = TimeSpan.Zero;
//             public TimeSpan ToleranceCheckInterval { get; set; } = TimeSpan.FromMilliseconds(100);

//             public long CreateCount;
//             public long DestroyCount;
//             public long IdleCheckCount;
//             public long GetTimeoutCount;

//             public virtual StressResource OnCreate()
//             {
//                 Interlocked.Increment(ref CreateCount);
//                 return new StressResource { ResourceId = Interlocked.Increment(ref _resourceSeed) };
//             }

//             public virtual void OnDestroy(StressResource obj)
//             {
//                 if (obj == null) return;
//                 Interlocked.Increment(ref DestroyCount);
//                 obj.Dispose();
//             }

//             public void OnGetTimeout()
//             {
//                 Interlocked.Increment(ref GetTimeoutCount);
//             }

//             public void OnGet(Object<StressResource> obj) { }

// #if net40
// #else
//             public Task OnGetAsync(Object<StressResource> obj)
//             {
//                 return TaskEx.FromResult(true);
//             }
// #endif

//             public void OnReturn(Object<StressResource> obj) { }

//             public bool OnCheckAvailable(Object<StressResource> obj)
//             {
//                 return obj?.Value != null;
//             }

//             public virtual void OnIdleCheck(Object<StressResource> obj)
//             {
//                 if (obj?.Value == null) return;
//                 Interlocked.Increment(ref IdleCheckCount);
//                 Thread.Sleep(1);
//             }

//             public void OnAvailable() { }
//             public void OnUnavailable() { }
//         }

//         sealed class FaultInjectionPolicy : StressPolicy
//         {
//             int _resourceSeed;
//             public long CreateFailureCount;
//             public long DestroyFailureCount;
//             public long IdleCheckFailureCount;

//             public override StressResource OnCreate()
//             {
//                 var call = Interlocked.Increment(ref CreateCount);
//                 if (call % 11 == 0)
//                 {
//                     Interlocked.Increment(ref CreateFailureCount);
//                     throw new InvalidOperationException("Injected OnCreate failure.");
//                 }
//                 return new StressResource { ResourceId = Interlocked.Increment(ref _resourceSeed) };
//             }

//             public override void OnDestroy(StressResource obj)
//             {
//                 if (obj == null) return;
//                 var call = Interlocked.Increment(ref DestroyCount);
//                 if (call % 13 == 0)
//                 {
//                     Interlocked.Increment(ref DestroyFailureCount);
//                     throw new InvalidOperationException("Injected OnDestroy failure.");
//                 }
//                 obj.Dispose();
//             }

//             public override void OnIdleCheck(Object<StressResource> obj)
//             {
//                 if (obj?.Value == null) return;
//                 var call = Interlocked.Increment(ref IdleCheckCount);
//                 if (call % 3 == 0)
//                 {
//                     Interlocked.Increment(ref IdleCheckFailureCount);
//                     throw new InvalidOperationException("Injected OnIdleCheck failure.");
//                 }
//                 Thread.Sleep(1);
//             }
//         }

//         public static void Test()
//         {
//             Console.WriteLine(Stress());
//         }

//         public static string StressMatrix()
//         {
//             var cases = new[]
//             {
//                 new[] { 8, 400, 2, 4, 101 },
//                 new[] { 16, 600, 3, 6, 202 },
//                 new[] { 32, 800, 4, 8, 303 },
//             };

//             var ret = new List<string>(cases.Length);
//             for (var index = 0; index < cases.Length; index++)
//             {
//                 var item = cases[index];
//                 ret.Add(Stress(item[0], item[1], item[2], item[3], item[4]));
//             }
//             return string.Join(Environment.NewLine, ret.ToArray());
//         }

//         public static string DetectStaleReturnRace(int attempts = 20)
//         {
//             if (attempts <= 0) throw new ArgumentOutOfRangeException(nameof(attempts));

//             for (var attempt = 1; attempt <= attempts; attempt++)
//             {
//                 var policy = new StressPolicy
//                 {
//                     PoolSize = 1,
//                     IdleTimeout = TimeSpan.Zero,
//                     IdleCheckTimeout = TimeSpan.Zero,
//                     IsThrowGetTimeoutException = false,
//                     SyncGetTimeout = TimeSpan.FromMilliseconds(80)
//                 };

//                 var pool = new ObjectPool<StressResource>(policy);
//                 var firstLease = pool.Get(TimeSpan.FromSeconds(1));
//                 pool.Return(firstLease);

//                 var borrowerReady = new ManualResetEventSlim(false);
//                 var releaseBorrower = new ManualResetEventSlim(false);
//                 Object<StressResource> activeLease = null;
//                 Exception borrowerError = null;

//                 var borrowerTask = TaskEx.Run(() =>
//                 {
//                     try
//                     {
//                         activeLease = pool.Get(TimeSpan.FromSeconds(1));
//                         borrowerReady.Set();
//                         releaseBorrower.Wait();
//                         pool.Return(activeLease);
//                     }
//                     catch (Exception ex)
//                     {
//                         borrowerError = ex;
//                         borrowerReady.Set();
//                     }
//                 });

//                 borrowerReady.Wait(TimeSpan.FromSeconds(2));
//                 if (borrowerError != null)
//                 {
//                     pool.Dispose();
//                     throw new AggregateException("Borrower task failed while preparing stale-return race.", borrowerError);
//                 }

//                 pool.Return(firstLease);

//                 var duplicateLease = pool.Get(TimeSpan.FromMilliseconds(50));
//                 if (duplicateLease != null)
//                 {
//                     releaseBorrower.Set();
//                     try { borrowerTask.Wait(); } catch { }
//                     try { pool.Return(duplicateLease); } catch { }
//                     pool.Dispose();
//                     throw new InvalidOperationException($"Stale return race reproduced on attempt {attempt}. A previously returned handle was able to return the object again after it had been re-leased.");
//                 }

//                 releaseBorrower.Set();
//                 borrowerTask.Wait();
//                 pool.Dispose();
//             }

//             return $"No stale-return race reproduced in {attempts} attempts.";
//         }

//         public static string ProbeIdleMaintenance()
//         {
//             var policy = new StressPolicy
//             {
//                 PoolSize = 1,
//                 IdleTimeout = TimeSpan.FromMilliseconds(100),
//                 IdleCheckTimeout = TimeSpan.FromMilliseconds(25),
//                 IsThrowGetTimeoutException = true,
//                 SyncGetTimeout = TimeSpan.FromMilliseconds(100)
//             };
//             var pool = new ObjectPool<StressResource>(policy);
//             try
//             {
//                 var lease = pool.Get(TimeSpan.FromMilliseconds(100));
//                 if (lease == null) throw new Exception("probe get returned null");
//                 pool.Return(lease);

//                 Thread.Sleep(policy.IdleCheckTimeout + TimeSpan.FromMilliseconds(10));
//                 pool.AutoFree();

//                 var afterCheckIdleCount = policy.IdleCheckCount;
//                 var afterCheckDestroyCount = policy.DestroyCount;

//                 Thread.Sleep(policy.IdleTimeout + TimeSpan.FromMilliseconds(10));
//                 pool.AutoFree();

//                 return $"Probe idle maintenance | idleChecks: {afterCheckIdleCount}, destroysAfterCheck: {afterCheckDestroyCount}, finalDestroys: {policy.DestroyCount}";
//             }
//             finally
//             {
//                 pool.Dispose();
//             }
//         }

//         public static string ProbeFaultInjectionIdleMaintenance()
//         {
//             var policy = new FaultInjectionPolicy
//             {
//                 PoolSize = 1,
//                 IdleTimeout = TimeSpan.FromMilliseconds(180),
//                 IdleCheckTimeout = TimeSpan.FromMilliseconds(25),
//                 IsThrowGetTimeoutException = true,
//                 SyncGetTimeout = TimeSpan.FromMilliseconds(100)
//             };
//             var pool = new ObjectPool<StressResource>(policy);
//             try
//             {
//                 var lease = pool.Get(TimeSpan.FromMilliseconds(100));
//                 if (lease == null) throw new Exception("fault probe get returned null");
//                 pool.Return(lease);

//                 for (var attempt = 0; attempt < 4; attempt++)
//                 {
//                     Thread.Sleep(policy.IdleCheckTimeout + TimeSpan.FromMilliseconds(10));
//                     pool.AutoFree();
//                 }

//                 return $"Probe fault idle maintenance | idleChecks: {policy.IdleCheckCount}, idleCheckFailures: {policy.IdleCheckFailureCount}, destroys: {policy.DestroyCount}";
//             }
//             finally
//             {
//                 pool.Dispose();
//             }
//         }

//         public static string FaultInjectionStress(int workerCount = 16, int iterationsPerWorker = 500, int autoFreeWorkerCount = 3, int poolSize = 6)
//         {
//             if (workerCount <= 0) throw new ArgumentOutOfRangeException(nameof(workerCount));
//             if (iterationsPerWorker <= 0) throw new ArgumentOutOfRangeException(nameof(iterationsPerWorker));
//             if (autoFreeWorkerCount <= 0) throw new ArgumentOutOfRangeException(nameof(autoFreeWorkerCount));
//             if (poolSize <= 0) throw new ArgumentOutOfRangeException(nameof(poolSize));

//             var policy = new FaultInjectionPolicy
//             {
//                 PoolSize = poolSize,
//                 IdleTimeout = TimeSpan.FromMilliseconds(180),
//                 IdleCheckTimeout = TimeSpan.FromMilliseconds(25),
//                 SyncGetTimeout = TimeSpan.FromMilliseconds(250),
//                 IsThrowGetTimeoutException = true
//             };
//             var pool = new ObjectPool<StressResource>(policy);
//             var start = new ManualResetEventSlim(false);
//             var activeObjects = new ConcurrentDictionary<int, byte>();
//             var errors = new ConcurrentQueue<Exception>();
//             long successfulGets = 0;
//             long successfulReturns = 0;
//             long expectedFailures = 0;
//             int stopAutoFree = 0;
//             var stopwatch = Stopwatch.StartNew();
//             var workerTasks = new List<Task>(workerCount + autoFreeWorkerCount);

//             for (var attempt = 0; attempt < 8 && errors.IsEmpty && policy.IdleCheckFailureCount <= 0; attempt++)
//             {
//                 Object<StressResource> lease = null;
//                 try
//                 {
//                     lease = pool.Get(TimeSpan.FromMilliseconds(300));
//                     if (lease == null) throw new Exception("pool.Get returned null during warmup");
//                     Interlocked.Increment(ref successfulGets);
//                 }
//                 catch (Exception ex)
//                 {
//                     if (ex.Message.Contains("Injected OnCreate failure") || ex.Message.Contains("ObjectPool.Get() timeout"))
//                     {
//                         Interlocked.Increment(ref expectedFailures);
//                         continue;
//                     }
//                     errors.Enqueue(ex);
//                     break;
//                 }

//                 try
//                 {
//                     pool.Return(lease);
//                     Interlocked.Increment(ref successfulReturns);
//                 }
//                 catch (Exception ex)
//                 {
//                     errors.Enqueue(ex);
//                     break;
//                 }

//                 Thread.Sleep(policy.IdleCheckTimeout + TimeSpan.FromMilliseconds(10));
//                 try
//                 {
//                     pool.AutoFree();
//                 }
//                 catch (Exception ex)
//                 {
//                     errors.Enqueue(ex);
//                     break;
//                 }
//             }

//             for (var worker = 0; worker < workerCount; worker++)
//             {
//                 var workerIndex = worker;
//                 workerTasks.Add(TaskEx.Run(() =>
//                 {
//                     var random = new Random(5000 + workerIndex * 97);
//                     start.Wait();

//                     for (var iteration = 0; iteration < iterationsPerWorker; iteration++)
//                     {
//                         if (errors.IsEmpty == false) return;

//                         Object<StressResource> lease = null;
//                         Exception returnError = null;
//                         try
//                         {
//                             lease = pool.Get(TimeSpan.FromMilliseconds(300));
//                             if (lease == null) throw new Exception("pool.Get returned null");
//                             if (activeObjects.TryAdd(lease.Id, 0) == false)
//                                 throw new Exception($"Duplicate lease detected for object {lease.Id}");

//                             Interlocked.Increment(ref successfulGets);

//                             if ((workerIndex + iteration) % 19 == 0)
//                                 Thread.Sleep((int)policy.IdleCheckTimeout.TotalMilliseconds + 10);
//                             else
//                                 Thread.Sleep(random.Next(0, 3));
//                         }
//                         catch (Exception ex)
//                         {
//                             if (ex.Message.Contains("Injected OnCreate failure") || ex.Message.Contains("ObjectPool.Get() timeout"))
//                             {
//                                 Interlocked.Increment(ref expectedFailures);
//                                 continue;
//                             }
//                             errors.Enqueue(ex);
//                             return;
//                         }
//                         finally
//                         {
//                             if (lease != null)
//                             {
//                                 activeObjects.TryRemove(lease.Id, out var _);
//                                 try
//                                 {
//                                     pool.Return(lease, (workerIndex + iteration) % 41 == 0);
//                                     Interlocked.Increment(ref successfulReturns);
//                                 }
//                                 catch (Exception ex)
//                                 {
//                                     if (ex.Message.Contains("Injected OnCreate failure"))
//                                         Interlocked.Increment(ref expectedFailures);
//                                     else
//                                         returnError = ex;
//                                 }
//                             }
//                         }

//                         if (returnError != null)
//                         {
//                             errors.Enqueue(returnError);
//                             return;
//                         }
//                     }
//                 }));
//             }

//             for (var worker = 0; worker < autoFreeWorkerCount; worker++)
//             {
//                 workerTasks.Add(TaskEx.Run(() =>
//                 {
//                     start.Wait();
//                     while (Interlocked.CompareExchange(ref stopAutoFree, 0, 0) == 0)
//                     {
//                         try
//                         {
//                             pool.AutoFree();
//                         }
//                         catch (Exception ex)
//                         {
//                             errors.Enqueue(ex);
//                             return;
//                         }
//                         Thread.Sleep(1);
//                     }
//                 }));
//             }

//             start.Set();
//             Task.WaitAll(workerTasks.GetRange(0, workerCount).ToArray());

//             Interlocked.Exchange(ref stopAutoFree, 1);
//             Task.WaitAll(workerTasks.GetRange(workerCount, autoFreeWorkerCount).ToArray());

//             for (var attempt = 0; attempt < 24 && errors.IsEmpty && policy.DestroyFailureCount <= 0; attempt++)
//             {
//                 Object<StressResource> lease = null;
//                 try
//                 {
//                     lease = pool.Get(TimeSpan.FromMilliseconds(300));
//                     if (lease == null) throw new Exception("pool.Get returned null during deterministic destroy injection");
//                     pool.Return(lease);
//                     Interlocked.Increment(ref successfulReturns);
//                 }
//                 catch (Exception ex)
//                 {
//                     if (ex.Message.Contains("Injected OnCreate failure") || ex.Message.Contains("ObjectPool.Get() timeout"))
//                     {
//                         Interlocked.Increment(ref expectedFailures);
//                         continue;
//                     }
//                     errors.Enqueue(ex);
//                     break;
//                 }

//                 Thread.Sleep(policy.IdleTimeout + TimeSpan.FromMilliseconds(10));
//                 try
//                 {
//                     pool.AutoFree();
//                 }
//                 catch (Exception ex)
//                 {
//                     errors.Enqueue(ex);
//                     break;
//                 }
//             }

//             Thread.Sleep(policy.IdleCheckTimeout + TimeSpan.FromMilliseconds(20));
//             pool.AutoFree();
//             Thread.Sleep(policy.IdleTimeout + TimeSpan.FromMilliseconds(20));
//             pool.AutoFree();

//             stopwatch.Stop();

//             if (activeObjects.IsEmpty == false)
//                 errors.Enqueue(new Exception($"Active lease map not empty after fault-injection run: {activeObjects.Count}"));
//             if (policy.CreateFailureCount <= 0)
//                 errors.Enqueue(new Exception($"Fault injection did not trigger OnCreate failures. createCount={policy.CreateCount}, createFailures={policy.CreateFailureCount}, destroyCount={policy.DestroyCount}, idleChecks={policy.IdleCheckCount}, idleCheckFailures={policy.IdleCheckFailureCount}, expectedFailures={expectedFailures}"));
//             if (policy.DestroyFailureCount <= 0)
//                 errors.Enqueue(new Exception($"Fault injection did not trigger OnDestroy failures. destroyCount={policy.DestroyCount}, destroyFailures={policy.DestroyFailureCount}, idleChecks={policy.IdleCheckCount}, idleCheckFailures={policy.IdleCheckFailureCount}, expectedFailures={expectedFailures}"));
//             if (policy.IdleCheckFailureCount <= 0)
//                 errors.Enqueue(new Exception($"Fault injection did not trigger OnIdleCheck failures before burst traffic. idleChecks={policy.IdleCheckCount}, idleCheckFailures={policy.IdleCheckFailureCount}, destroyCount={policy.DestroyCount}, destroyFailures={policy.DestroyFailureCount}, createCount={policy.CreateCount}, createFailures={policy.CreateFailureCount}, expectedFailures={expectedFailures}"));

//             pool.Dispose();

//             if (errors.TryDequeue(out var firstError))
//                 throw new AggregateException("ObjectPool fault-injection stress failed.", firstError);

//             return $"ObjectPool fault injection OK | workers: {workerCount}, iterations: {iterationsPerWorker}, poolSize: {poolSize}, gets: {successfulGets}, returns: {successfulReturns}, expectedFailures: {expectedFailures}, createFailures: {policy.CreateFailureCount}, destroyFailures: {policy.DestroyFailureCount}, idleCheckFailures: {policy.IdleCheckFailureCount}, idleChecks: {policy.IdleCheckCount}, elapsedMs: {stopwatch.ElapsedMilliseconds}";
//         }

//         public static string FaultInjectionMatrix()
//         {
//             var cases = new[]
//             {
//                 new[] { 8, 250, 2, 4 },
//                 new[] { 16, 500, 3, 6 },
//                 new[] { 24, 700, 4, 8 },
//             };

//             var ret = new List<string>(cases.Length);
//             for (var index = 0; index < cases.Length; index++)
//             {
//                 var item = cases[index];
//                 ret.Add(FaultInjectionStress(item[0], item[1], item[2], item[3]));
//             }
//             return string.Join(Environment.NewLine, ret.ToArray());
//         }

//         public static string Stress(int workerCount = 32, int iterationsPerWorker = 2000, int autoFreeWorkerCount = 4, int poolSize = 8, int seed = 0)
//         {
//             if (workerCount <= 0) throw new ArgumentOutOfRangeException(nameof(workerCount));
//             if (iterationsPerWorker <= 0) throw new ArgumentOutOfRangeException(nameof(iterationsPerWorker));
//             if (autoFreeWorkerCount <= 0) throw new ArgumentOutOfRangeException(nameof(autoFreeWorkerCount));
//             if (poolSize <= 0) throw new ArgumentOutOfRangeException(nameof(poolSize));

//             var policy = new StressPolicy { PoolSize = poolSize };
//             var pool = new ObjectPool<StressResource>(policy);
//             var start = new ManualResetEventSlim(false);
//             var activeObjects = new ConcurrentDictionary<int, byte>();
//             var errors = new ConcurrentQueue<Exception>();
//             long successfulGets = 0;
//             long successfulReturns = 0;
//             int stopAutoFree = 0;
//             var stopwatch = Stopwatch.StartNew();

//             var workerTasks = new List<Task>(workerCount + autoFreeWorkerCount);

//             for (var worker = 0; worker < workerCount; worker++)
//             {
//                 var workerIndex = worker;
//                 workerTasks.Add(TaskEx.Run(() =>
//                 {
//                     var seedBase = seed == 0 ? Environment.TickCount : seed;
//                     var random = new Random(seedBase ^ (workerIndex * 397));
//                     start.Wait();

//                     for (var iteration = 0; iteration < iterationsPerWorker; iteration++)
//                     {
//                         if (errors.IsEmpty == false) return;

//                         Object<StressResource> lease = null;
//                         Exception returnError = null;
//                         try
//                         {
//                             lease = pool.Get(TimeSpan.FromSeconds(5));
//                             if (lease == null) throw new Exception("pool.Get returned null");
//                             if (activeObjects.TryAdd(lease.Id, 0) == false)
//                                 throw new Exception($"Duplicate lease detected for object {lease.Id}");

//                             Interlocked.Increment(ref successfulGets);

//                             var workDelay = random.Next(0, 4);
//                             if (workDelay > 0) Thread.Sleep(workDelay);
//                             if ((iteration & 31) == 0) Thread.SpinWait(5000);
//                             if ((workerIndex + iteration) % 97 == 0)
//                                 Thread.Sleep((int)policy.IdleCheckTimeout.TotalMilliseconds + 5);
//                         }
//                         catch (Exception ex)
//                         {
//                             errors.Enqueue(ex);
//                             return;
//                         }
//                         finally
//                         {
//                             if (lease != null)
//                             {
//                                 activeObjects.TryRemove(lease.Id, out var _);
//                                 try
//                                 {
//                                     pool.Return(lease, (workerIndex + iteration) % 113 == 0);
//                                     Interlocked.Increment(ref successfulReturns);
//                                 }
//                                 catch (Exception ex)
//                                 {
//                                     returnError = ex;
//                                 }
//                             }
//                         }

//                         if (returnError != null)
//                         {
//                             errors.Enqueue(returnError);
//                             return;
//                         }

//                         if ((workerIndex + iteration) % 29 == 0)
//                             Thread.Sleep(random.Next(0, 3));
//                     }
//                 }));
//             }

//             for (var worker = 0; worker < autoFreeWorkerCount; worker++)
//             {
//                 workerTasks.Add(TaskEx.Run(() =>
//                 {
//                     start.Wait();
//                     while (Interlocked.CompareExchange(ref stopAutoFree, 0, 0) == 0)
//                     {
//                         try
//                         {
//                             pool.AutoFree();
//                         }
//                         catch (Exception ex)
//                         {
//                             errors.Enqueue(ex);
//                             return;
//                         }
//                         Thread.Sleep(1);
//                     }
//                 }));
//             }

//             start.Set();
//             Task.WaitAll(workerTasks.GetRange(0, workerCount).ToArray());

//             Interlocked.Exchange(ref stopAutoFree, 1);
//             Task.WaitAll(workerTasks.GetRange(workerCount, autoFreeWorkerCount).ToArray());

//             if (activeObjects.IsEmpty == false)
//                 errors.Enqueue(new Exception($"Active lease map not empty after stress run: {activeObjects.Count}"));

//             Thread.Sleep(policy.IdleCheckTimeout + TimeSpan.FromMilliseconds(20));
//             pool.AutoFree();
//             Thread.Sleep(policy.IdleTimeout + TimeSpan.FromMilliseconds(20));
//             pool.AutoFree();

//             stopwatch.Stop();

//             if (policy.IdleCheckCount <= 0)
//                 errors.Enqueue(new Exception("Idle keepalive did not execute during stress test."));
//             if (policy.DestroyCount <= 0)
//                 errors.Enqueue(new Exception("Idle release did not execute during stress test."));
//             if (policy.GetTimeoutCount > 0)
//                 errors.Enqueue(new Exception($"Unexpected get timeout count: {policy.GetTimeoutCount}"));

//             pool.Dispose();

//             if (errors.TryDequeue(out var firstError))
//                 throw new AggregateException("ObjectPool stress test failed.", firstError);

//             return $"ObjectPool stress OK | workers: {workerCount}, iterations: {iterationsPerWorker}, poolSize: {poolSize}, gets: {successfulGets}, returns: {successfulReturns}, created: {policy.CreateCount}, destroyed: {policy.DestroyCount}, idleChecks: {policy.IdleCheckCount}, elapsedMs: {stopwatch.ElapsedMilliseconds}";
//         }
//     }
// }
using System;
using System.Threading;

namespace FreeRedis.Internal.ObjectPool
{

    public class Object<T> : IDisposable
    {
        const int StateLeased = 0;
        const int StateFree = 1;
        const int StateTransition = 2;

        Object<T> _root;
        IObjectPool<T> _pool;
        int _id;
        T _value;
        internal long _getTimes;
        DateTime _lastGetTime;
        DateTime _lastGetTimeCopy;
        DateTime _lastReturnTime;
        DateTime _lastKeepAliveTime;
        DateTime _createTime = DateTime.Now;
        int _lastGetThreadId;
        int _lastReturnThreadId;
        int _state = StateLeased;
        int _leaseVersion;

        internal Object<T> RootObject => _root ?? this;

        public static Object<T> InitWith(IObjectPool<T> pool, int id, T value)
        {
            return new Object<T>
            {
                _pool = pool,
                _id = id,
                _value = value,
                _lastGetThreadId = Thread.CurrentThread.ManagedThreadId,
                _lastGetTime = DateTime.Now,
                _lastGetTimeCopy = DateTime.Now,
                _lastKeepAliveTime = DateTime.Now,
                _leaseVersion = 1
            };
        }

        /// <summary>
        /// 所属对象池
        /// </summary>
        public IObjectPool<T> Pool { get => RootObject._pool; internal set => RootObject._pool = value; }

        /// <summary>
        /// 在对象池中的唯一标识
        /// </summary>
        public int Id { get => RootObject._id; internal set => RootObject._id = value; }
        /// <summary>
        /// 资源对象
        /// </summary>
        public T Value { get => RootObject._value; internal set => RootObject._value = value; }

        /// <summary>
        /// 被获取的总次数
        /// </summary>
        public long GetTimes => RootObject._getTimes;

        /// 最后获取时的时间
        public DateTime LastGetTime { get => RootObject._lastGetTime; internal set => RootObject._lastGetTime = value; }
        public DateTime LastGetTimeCopy { get => RootObject._lastGetTimeCopy; internal set => RootObject._lastGetTimeCopy = value; }

        /// <summary>
        /// 最后归还时的时间
        /// </summary>
        public DateTime LastReturnTime { get => RootObject._lastReturnTime; internal set => RootObject._lastReturnTime = value; }

        /// <summary>
        /// 最后一次空闲保活时间
        /// </summary>
        public DateTime LastKeepAliveTime { get => RootObject._lastKeepAliveTime; internal set => RootObject._lastKeepAliveTime = value; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get => RootObject._createTime; internal set => RootObject._createTime = value; }

        /// <summary>
        /// 最后获取时的线程id
        /// </summary>
        public int LastGetThreadId { get => RootObject._lastGetThreadId; internal set => RootObject._lastGetThreadId = value; }

        /// <summary>
        /// 最后归还时的线程id
        /// </summary>
        public int LastReturnThreadId { get => RootObject._lastReturnThreadId; internal set => RootObject._lastReturnThreadId = value; }

        public override string ToString()
        {
            return $"{this.Value}, Times: {this.GetTimes}, ThreadId(R/G): {this.LastReturnThreadId}/{this.LastGetThreadId}, Time(R/G): {this.LastReturnTime.ToString("yyyy-MM-dd HH:mm:ss:ms")}/{this.LastGetTime.ToString("yyyy-MM-dd HH:mm:ss:ms")}";
        }

        /// <summary>
        /// 释放 Value 值
        /// </summary>
        public void ReleaseValue()
        {
            var root = RootObject;
            if (root._value != null)
            {
                try { root._pool.Policy.OnDestroy(root._value); } catch { }
                try { (root._value as IDisposable)?.Dispose(); } catch { }
            }
            root._value = default(T);
            root._lastReturnTime = DateTime.Now;
            root._lastKeepAliveTime = root._lastReturnTime;
        }

        /// <summary>
        /// 重置 Value 值
        /// </summary>
        public void ResetValue()
        {
            var root = RootObject;
            ReleaseValue();
            root._value = root._pool.Policy.OnCreate();
            root._lastReturnTime = DateTime.Now;
            root._lastKeepAliveTime = root._lastReturnTime;
        }

        internal bool TryBeginReturn()
        {
            var root = RootObject;
            if (_root != null && _leaseVersion != Thread.VolatileRead(ref root._leaseVersion))
                return false;
            return Interlocked.CompareExchange(ref root._state, StateTransition, StateLeased) == StateLeased;
        }

        internal bool TryReserveFree()
        {
            var root = RootObject;
            return Interlocked.CompareExchange(ref root._state, StateTransition, StateFree) == StateFree;
        }

        internal Object<T> ActivateLease()
        {
            var root = RootObject;
            var leaseVersion = Interlocked.Increment(ref root._leaseVersion);
            Thread.VolatileWrite(ref root._state, StateLeased);
            return new Object<T>
            {
                _root = root,
                _leaseVersion = leaseVersion
            };
        }

        internal void MarkLeased()
        {
            Thread.VolatileWrite(ref RootObject._state, StateLeased);
        }

        internal void MarkFree()
        {
            Thread.VolatileWrite(ref RootObject._state, StateFree);
        }

        internal bool IsFree => Thread.VolatileRead(ref RootObject._state) == StateFree;

        public void Dispose()
        {
            Pool?.Return(this);
        }
    }
}
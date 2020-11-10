using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis
{
    public enum NoticeType
    {
        Call, Info
    }
    public class NoticeEventArgs : EventArgs
    {
        public NoticeType NoticeType { get; }
        public Exception Exception { get; }
        public string Log { get; }
        public object Tag { get; }

        public NoticeEventArgs(NoticeType noticeType, Exception exception, string log, object tag)
        {
            this.NoticeType = noticeType;
            this.Exception = exception;
            this.Log = log;
            this.Tag = tag;
        }
    }
    public class ConnectedEventArgs : EventArgs
    {
        public string Host { get; }
        public RedisClientPool Pool { get; }
        public RedisClient Client { get; }

        public ConnectedEventArgs(string host, RedisClientPool pool, RedisClient cli)
        {
            this.Host = host;
            this.Pool = pool;
            this.Client = cli;
        }
    }
    public class UnavailableEventArgs : EventArgs
    {
        public string Host { get; }
        public RedisClientPool Pool { get; }

        public UnavailableEventArgs(string host, RedisClientPool pool)
        {
            this.Host = host;
            this.Pool = pool;
        }
    }

    public interface IInterceptor
    {
        void Before(InterceptorBeforeEventArgs args);
        void After(InterceptorAfterEventArgs args);
    }
    public class InterceptorBeforeEventArgs
    {
        public RedisClient Client { get; }
        public CommandPacket Command { get; }
        public Type ValueType { get; }

        public InterceptorBeforeEventArgs(RedisClient cli, CommandPacket cmd, Type valueType)
        {
            this.Client = cli;
            this.Command = cmd;
            this.ValueType = valueType;
        }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                this.ValueIsChanged = true;
            }
        }
        private object _value;
        public bool ValueIsChanged { get; private set; }
    }
    public class InterceptorAfterEventArgs
    {
        public RedisClient Client { get; }
        public CommandPacket Command { get; }
        public Type ValueType { get; }

        public object Value { get; }
        public Exception Exception { get; }
        public long ElapsedMilliseconds { get; }

        public InterceptorAfterEventArgs(RedisClient cli, CommandPacket cmd, Type valueType, object value, Exception exception, long elapsedMilliseconds)
        {
            this.Client = cli;
            this.Command = cmd;
            this.ValueType = valueType;
            this.Value = value;
            this.Exception = exception;
            this.ElapsedMilliseconds = elapsedMilliseconds;
        }
    }

    class NoticeCallInterceptor : IInterceptor
    {
        RedisClient _cli;
        public NoticeCallInterceptor(RedisClient cli)
        {
            _cli = cli;
        }

        public void After(InterceptorAfterEventArgs args)
        {
            string log;
            if (args.Exception != null) log = $"{args.Exception.Message}";
            else if (args.Value is Array array)
            {
                var sb = new StringBuilder().Append("[");
                var itemindex = 0;
                foreach (var item in array)
                {
                    if (itemindex++ > 0) sb.Append(", ");
                    sb.Append(item.ToInvariantCultureToString());
                }
                log = sb.Append("]").ToString();
                sb.Clear();
            }
            else
                log = $"{args.Value.ToInvariantCultureToString()}";
            _cli.OnNotice(null, new NoticeEventArgs(
                NoticeType.Call,
                args.Exception,
                $"{(args.Command.WriteHost ?? "Not connected").PadRight(21)} > {args.Command}\r\n{log}\r\n({args.ElapsedMilliseconds}ms)\r\n",
                args.Value));
        }

        public void Before(InterceptorBeforeEventArgs args)
        {
        }
    }
}

using System;
using System.Diagnostics;
using System.Text;
using FreeRedis.Internal;

namespace FreeRedis
{
    public enum NoticeType
    {
        Call,
        Info,
        Event
    }

    public class NoticeEventArgs : EventArgs
    {
        public NoticeEventArgs(NoticeType noticeType, Exception exception, string log, object tag)
        {
            NoticeType = noticeType;
            Exception = exception;
            Log = log;
            Tag = tag;
        }

        public NoticeType NoticeType { get; }
        public Exception Exception { get; }
        public string Log { get; }
        public object Tag { get; }
    }

    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(string host, RedisClientPool pool, RedisClient cli)
        {
            Host = host;
            Pool = pool;
            Client = cli;
        }

        public string Host { get; }
        public RedisClientPool Pool { get; }
        public RedisClient Client { get; }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(string host, RedisClientPool pool, RedisClient cli)
        {
            Host = host;
            Pool = pool;
            Client = cli;
        }

        public string Host { get; }
        public RedisClientPool Pool { get; }
        public RedisClient Client { get; }
    }

    public class UnavailableEventArgs : EventArgs
    {
        public UnavailableEventArgs(string host, RedisClientPool pool)
        {
            Host = host;
            Pool = pool;
        }

        public string Host { get; }
        public RedisClientPool Pool { get; }
    }

    public interface IInterceptor
    {
        void Before(InterceptorBeforeEventArgs args);
        void After(InterceptorAfterEventArgs args);
    }

    public class InterceptorBeforeEventArgs
    {
        private object _value;

        public InterceptorBeforeEventArgs(RedisClient cli, CommandPacket cmd, Type valueType)
        {
            Client = cli;
            Command = cmd;
            ValueType = valueType;
        }

        public long? OperationTimestamp { get; set; }
        public RedisClient Client { get; }
        public CommandPacket Command { get; }
        public Type ValueType { get; }
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueIsChanged = true;
            }
        }
        public bool ValueIsChanged { get; private set; }
    }

    public class InterceptorAfterEventArgs
    {
        public InterceptorAfterEventArgs(RedisClient cli, CommandPacket cmd, Type valueType, object value,
            Exception exception, long elapsedMilliseconds)
        {
            Client = cli;
            Command = cmd;
            ValueType = valueType;
            Value = value;
            Exception = exception;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        public long? OperationTimestamp { get; set; }
        public RedisClient Client { get; }
        public CommandPacket Command { get; }
        public Type ValueType { get; }
        public object Value { get; }
        public Exception Exception { get; }
        public long ElapsedMilliseconds { get; }
    }

    internal class NoticeCallInterceptor : IInterceptor
    {
#if NETSTANDARD2_0_OR_GREATER
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(FreeRedisDiagnosticListenerNames.DiagnosticListenerName);
#endif
        private readonly RedisClient _cli;

        public NoticeCallInterceptor(RedisClient cli)
        {
            _cli = cli;
        }

        public void After(InterceptorAfterEventArgs args)
        {
#if NETSTANDARD2_0_OR_GREATER
            args.OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_diagnosticListener.IsEnabled(FreeRedisDiagnosticListenerNames.NoticeCallAfter))
                _diagnosticListener.Write(FreeRedisDiagnosticListenerNames.NoticeCallAfter, args);
#endif

            string log;
            if (args.Exception != null)
            {
                log = $"{args.Exception.Message}";
            }
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
            {
                log = $"{args.Value.ToInvariantCultureToString()}";
            }

            _cli.OnNotice(null, new NoticeEventArgs(
                NoticeType.Call,
                args.Exception,
                $"{(args.Command.WriteTarget ?? "Not connected").PadRight(21)} > {args.Command}\r\n{log}\r\n({args.ElapsedMilliseconds}ms)\r\n",
                args.Value));
        }

        public void Before(InterceptorBeforeEventArgs args)
        {
#if NETSTANDARD2_0_OR_GREATER
            args.OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_diagnosticListener.IsEnabled(FreeRedisDiagnosticListenerNames.NoticeCallBefore))
                _diagnosticListener.Write(FreeRedisDiagnosticListenerNames.NoticeCallBefore, args);
#endif
        }
    }
}
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FreeRedis.OpenTelemetry
{

    public class DiagnosticListener : IObserver<KeyValuePair<string, object?>>
    {
        public const string SourceName = "FreeRedis.OpenTelemetry";

        private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");

        /// <summary>Notifies the observer that the provider has finished sending push-based notifications.</summary>
        public void OnCompleted()
        {
        }

        /// <summary>Notifies the observer that the provider has experienced an error condition.</summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>Provides the observer with new data.</summary>
        /// <param name="evt">The current notification information.</param>
        public void OnNext(KeyValuePair<string, object?> evt)
        {
            //https://opentelemetry.io/docs/specs/semconv/database/redis/
            switch (evt.Key)
            {
                case FreeRedisDiagnosticListenerNames.NoticeCallBefore:
                    {
                        var eventData = (InterceptorBeforeEventArgs)evt.Value!;
                        var activity = ActivitySource.StartActivity("redis command execute: " + eventData.Command);
                        if (activity != null)
                        {
                            activity.SetTag("db.system", "redis");
                            activity.SetTag("db.operation.name", eventData.Command._command);
                            activity.SetTag("db.query.text", eventData.Command);
                            //Activity.Current?.SetTag("network.peer.address", ip);
                            //Activity.Current?.SetTag("network.peer.port", port);

                            activity.AddEvent(new ActivityEvent("redis command execute start",
                                DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));
                        }
                    }
                    break;
                case FreeRedisDiagnosticListenerNames.NoticeCallAfter:
                    {
                        var eventData = (InterceptorAfterEventArgs)evt.Value!;
                        var writeTarget = eventData.Command.WriteTarget;
                        if (!string.IsNullOrEmpty(writeTarget))
                        {
                            var parts = writeTarget.Split(new[] { ':', '/' }, StringSplitOptions.RemoveEmptyEntries);
                            var ip = parts[0];
                            var port = int.Parse(parts[1]);
                            var dbIndex = int.Parse(parts[2]);

                            Activity.Current?.SetTag("server.address", ip);
                            Activity.Current?.SetTag("server.port", port);
                            Activity.Current?.SetTag("db.namespace", dbIndex);
                        }
                        var tags = new ActivityTagsCollection { new("free_redis.duration", eventData.ElapsedMilliseconds) };
                        if (eventData.Exception != null)
                        {
                            Activity.Current?.SetStatus(Status.Error.WithDescription(eventData.Exception.Message));
                            tags.Add(new("error.type", eventData.Exception.Message));
                        }
                        Activity.Current?.AddEvent(new ActivityEvent("redis command executed",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value), tags)
                        );

                        Activity.Current?.Stop();
                    }
                    break;
            }
        }
    }
}
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace FreeRedis.OpenTelemetry;

public class DiagnosticListener : IObserver<KeyValuePair<string, object?>>
{
    public const string SourceName = "FreeRedis.OpenTelemetry";

    private const string OperateNamePrefix = "FreeRedis/";
    private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

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
                        activity.SetTag("db.namespace", 1);//TODO 不知道怎么获取db
                        activity.SetTag("db.operation.name", eventData.Command._command);
                        activity.SetTag("db.query.text", eventData.Command);
                        //activity.SetTag("server.address", eventData.Operation);
                        //activity.SetTag("server.port", 6379);
                        //activity.SetTag("network.peer.address", eventData.Operation);
                        //activity.SetTag("network.peer.port", eventData.Operation);

                        activity.AddEvent(new ActivityEvent("redis command execute start",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                    }
                }
                break;
            case FreeRedisDiagnosticListenerNames.NoticeCallAfter:
                {
                    var eventData = (InterceptorAfterEventArgs)evt.Value!;
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
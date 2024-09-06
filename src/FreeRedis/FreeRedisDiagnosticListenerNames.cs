namespace FreeRedis
{
    public static class FreeRedisDiagnosticListenerNames
    {
        private const string FreeRedisPrefix = "FreeRedis.";

        //Tracing
        public const string DiagnosticListenerName = "FreeRedisDiagnosticListener";

        public const string NoticeCallAfter = FreeRedisPrefix + "NoticeCallAfter";
        public const string NoticeCallBefore = FreeRedisPrefix + "NoticeCallBefore";
    }
}
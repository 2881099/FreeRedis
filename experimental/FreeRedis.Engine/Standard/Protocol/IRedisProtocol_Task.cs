public abstract class IRedisProtocal<T> : IRedisProtocol
{
    protected readonly TaskCompletionSource<T> Task;
    public IRedisProtocal(Action<string>? logger) : base(logger)
    {
        Task = new TaskCompletionSource<T>();
    }

    public Task<T> WaitTask { get { return Task.Task; } }
}


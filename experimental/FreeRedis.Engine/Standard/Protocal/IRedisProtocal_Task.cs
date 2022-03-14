public abstract class IRedisProtocal<T> : IRedisProtocal
{
    protected readonly TaskCompletionSource<T> Task;
    public IRedisProtocal()
    {
        Task = new TaskCompletionSource<T>();
    }

    public Task<T> WaitTask { get { return Task.Task; } }
}


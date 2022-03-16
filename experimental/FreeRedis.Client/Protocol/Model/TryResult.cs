public class TryResult<T>
{
    public TryResult(bool success,T? result)
    {
        Success = success;
        Result = result;
    }
    public readonly bool Success;
    public readonly T? Result;
}


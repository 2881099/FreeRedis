public abstract class IRedisProtocal
{
    protected byte[] ReadBuffer = default!;
    /// <summary>
    /// 将值通过协议转换为 byte 发送到服务器. 
    /// </summary>
    /// <returns>协议数据流</returns>
    public virtual byte[] GetSendBytes()
    {
        return ReadBuffer;
    }
    /// <summary>
    /// 根据获取到的比特流解析结果,如果需要继续处理下一个流,返回 false 即可.
    /// </summary>
    /// <param name="recvBytes">从服务器收到的数据流切片(连续)</param>
    /// <param name="offset">下一条协议起始位置</param>
    public abstract bool GetInstanceFromBytes(in ReadOnlyMemory<byte> recvBytes, ref int offset);

}

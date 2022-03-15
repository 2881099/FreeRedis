using System.Buffers;
using System.Text;

public abstract class IRedisProtocol
{
    public const byte TAIL = 10;
    public const byte OK_HEAD = 43;
    public const byte ERROR_HEAD = 45;

    protected bool IsOKRemainData;
    protected bool IsErrorRemainData;
    protected byte[] ReadBuffer = default!;

    private StringBuilder? _error;

    private readonly Action<string>? _writeLogger; 
    public IRedisProtocol(Action<string>? writeLogger)
    {
        _writeLogger = writeLogger;
    }
    protected void Error(in ReadOnlySpan<byte> errorStream)
    {
        if (_error == null)
        {
            _error = new StringBuilder();
        }
        _error.AppendLine(Encoding.UTF8.GetString(errorStream));
    }

    /// <summary>
    /// 将值通过协议转换为 byte 发送到服务器. 
    /// </summary>
    /// <returns>协议数据流</returns>
    public virtual byte[] GetSendBytes()
    {
        return ReadBuffer;
    }

    public string? ErrorMessage { get { return _error == null? null : _error.ToString(); } }

    public virtual bool HandleBytes(ref SequenceReader<byte> recvReader)
    {
        if (IsOKRemainData)
        {

            return HandleAfterOkBytes(ref recvReader);

        }
        else if (IsErrorRemainData)
        {

            return HandleAfterErrorBytes(ref recvReader);

        }
        else if (recvReader.IsNext(ERROR_HEAD, true))
        {
            SetErrorDefaultResult();
            if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
            {
                Error(requestLine);
                if (_writeLogger!=null)
                {
                    _writeLogger(ErrorMessage!);
                }
                return true;
            }
            else
            {
                recvReader.AdvanceToEnd();
                //粘包处理(前段)
                this.PostToAfterError();
                return false;
            }
        }
        else
        {

            return HandleOkBytes(ref recvReader);

        }
    }

    protected abstract void SetErrorDefaultResult();

    protected void PostToAfterOk()
    {
        IsOKRemainData = true;
    }

    protected void PostToAfterError()
    {
        IsErrorRemainData = true;
    }

    /// <summary>
    /// 根据获取到的比特流解析结果,如果需要继续处理下一个流,返回 false 即可.
    /// </summary>
    /// <param name="recvBytes">从服务器收到的数据流切片(连续)</param>
    /// <param name="offset">下一条协议起始位置</param>
    protected abstract bool HandleOkBytes(ref SequenceReader<byte> recvReader);

    protected virtual bool HandleAfterOkBytes(ref SequenceReader<byte> recvReader)
    {
        recvReader.AdvancePast(TAIL);
        return true;
    }

    protected virtual bool HandleAfterErrorBytes(ref SequenceReader<byte> recvReader)
    {
        if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
        {
            Error(in requestLine);
            if (_writeLogger != null)
            {
                _writeLogger(ErrorMessage!);
            }
            return true;
        }
        throw new Exception("未找到后续错误边界!");
    }

}

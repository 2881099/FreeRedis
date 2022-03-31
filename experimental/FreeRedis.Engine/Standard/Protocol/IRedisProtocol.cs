using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

public abstract class IRedisProtocol
{
    public const byte TAIL = 10;
    public const byte OK_HEAD = 43;
    public const byte OK_DATA = 36;
    public const byte ERROR_HEAD = 45;
    public const byte OK_BATCH_DATA = 42;
    protected static readonly Encoder Utf8Encoder;

    public static readonly byte[] SplitField;


    static IRedisProtocol()
    {
        SplitField = Encoding.UTF8.GetBytes("\r\n");
        Utf8Encoder = Encoding.UTF8.GetEncoder();
    }

    public byte[] ReadBuffer = default!;

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

    public string? ErrorMessage { get { return _error == null ? null : _error.ToString(); } }

    public virtual ProtocolContinueResult HandleBytes(ref SequenceReader<byte> recvReader)
    {
        //recvReader.IsNext(ERROR_HEAD, false)
        if (!recvReader.IsNext(ERROR_HEAD, false))
        {
            return HandleOkBytes(ref recvReader);
        }
        else if (recvReader.TryReadTo(out ReadOnlySpan<byte> requestLine, TAIL, true))
        {
            SetErrorDefaultResult();
            if (_writeLogger != null)
            {
                Error(requestLine);
                _writeLogger(ErrorMessage!);
            }
            return ProtocolContinueResult.Completed;
        }
        return ProtocolContinueResult.Wait;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void SetErrorDefaultResult();


    /// <summary>
    /// 根据获取到的比特流解析结果,如果需要继续处理下一个流,返回 false 即可.
    /// </summary>
    /// <param name="recvBytes">从服务器收到的数据流切片(连续)</param>
    /// <param name="offset">下一条协议起始位置</param>
    protected abstract ProtocolContinueResult HandleOkBytes(ref SequenceReader<byte> recvReader);


}

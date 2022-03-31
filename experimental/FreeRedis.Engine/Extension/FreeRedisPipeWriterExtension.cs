using System.IO.Pipelines;
using System.Text;


public static class FreeRedisPipeWriterExtension
{
    private static readonly Encoder _utf8Encoder;

    static FreeRedisPipeWriterExtension()
    {
        _utf8Encoder = Encoding.UTF8.GetEncoder();
    }
    public static void WriteUtf8String(this PipeWriter writer, string data)
    {
        var chars = data.AsSpan();
        if (chars.Length <= 1048576)
        {
            int sizeHint = _utf8Encoder.GetByteCount(chars, true);
            Span<byte> span = writer.GetSpan(sizeHint);
            _utf8Encoder.Convert(chars, span, true, out _, out var bytesUsed2, out _);
            writer.Advance(bytesUsed2);
        }
        else
        {
            do
            {
                int sizeHint = _utf8Encoder.GetByteCount(chars[..1048576], flush: false);
                Span<byte> span = writer.GetSpan(sizeHint);
                _utf8Encoder.Convert(chars, span, true, out var charsUsed, out var bytesUsed2, out _);
                chars = chars.Slice(charsUsed);
                writer.Advance(bytesUsed2);
            }
            while (!chars.IsEmpty);
        }
    }
}


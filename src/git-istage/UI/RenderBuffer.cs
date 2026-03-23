using System.Text;

namespace GitIStage.UI;

internal sealed class RenderBuffer
{
    private static ThreadLocal<RenderBuffer> Current = new(() => new RenderBuffer());

    private readonly StringBuilder _buffer = new();

    public static RenderBuffer Begin()
    {
        var buffer = Current.Value!;
        buffer._buffer.Clear();
        return buffer;
    }

    public void Flush()
    {
        Console.Out.Write(_buffer);
    }

    public void Write(char value)
    {
        _buffer.Append(value);
    }

    public void Write(ReadOnlySpan<char> value)
    {
        _buffer.Append(value);
    }
}

namespace GitIStage.Text;

public static class TextSpanExtensions
{
    public static ReadOnlySpan<char> AsSpan(this string text, TextSpan span)
    {
        return text.AsSpan(span.Start, span.Length);
    }

    public static ReadOnlySpan<char> Slice(this ReadOnlySpan<char> text, TextSpan span)
    {
        return text.Slice(span.Start, span.Length);
    }
}
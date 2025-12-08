namespace GitIStage.Patching.Text;

public readonly struct TextSpan
{
    public TextSpan(int start, int length)
    {
        ThrowIfNegative(start);
        ThrowIfNegative(Length);

        Start = start;
        Length = length;
    }

    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    public static TextSpan FromBounds(int start, int end)
    {
        var length = end - start;
        return new TextSpan(start, length);
    }

    public bool OverlapsWith(TextSpan span)
    {
        return Start < span.End &&
               End > span.Start;
    }

    public bool Contains(TextSpan span)
    {
        var lastPosition = span.Length == 0 ? span.Start : span.End - 1;
        return Contains(span.Start) && Contains(lastPosition);
    }

    public bool Contains(int position)
    {
        return Start <= position && position < End;
    }

    public override string ToString() => $"{Start}..{End}";
}
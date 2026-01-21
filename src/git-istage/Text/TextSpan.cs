namespace GitIStage.Text;

public readonly struct TextSpan : IEquatable<TextSpan>
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
        ThrowIfNegative(start);
        ThrowIfLessThan(end, start);

        var length = end - start;
        return new TextSpan(start, length);
    }

    public TextSpan RelativeTo(int position)
    {
        return new TextSpan(Start - position, Length);
    }

    public bool OverlapsWith(TextSpan other)
    {
        return Start < other.End &&
               End > other.Start;
    }

    public bool Contains(TextSpan span)
    {
        var lastPosition = span.Length == 0 ? span.Start : span.End - 1;
        return Contains(span.Start) && Contains(lastPosition);
    }

    public bool Contains(int position)
    {
        ThrowIfNegative(position);

        return Start <= position && position < End;
    }

    public bool Equals(TextSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !left.Equals(right);
    }

    public override string ToString() => $"{Start}..{End}";
}
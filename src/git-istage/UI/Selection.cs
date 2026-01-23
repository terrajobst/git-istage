namespace GitIStage.UI;

internal readonly struct Selection : IEquatable<Selection>
{
    public Selection(int startLine, int count, bool atEnd = false)
    {
        ThrowIfNegative(startLine);
        ThrowIfLessThan(count, 0);

        StartLine = startLine;
        Count = count;
        AtEnd = atEnd;
    }

    public int StartLine { get; }

    public int EndLine => StartLine + Count;

    public int Count { get; }

    public bool AtEnd { get; }

    public bool Contains(int lineIndex)
    {
        return StartLine <= lineIndex && lineIndex <= EndLine;
    }

    public bool Equals(Selection other)
    {
        return StartLine == other.StartLine && Count == other.Count && AtEnd == other.AtEnd;
    }

    public override bool Equals(object? obj)
    {
        return obj is Selection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartLine, Count, AtEnd);
    }

    public static bool operator ==(Selection left, Selection right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Selection left, Selection right)
    {
        return !left.Equals(right);
    }
}
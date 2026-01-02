namespace GitIStage.Text;

public readonly struct TextPosition : IEquatable<TextPosition>
{
    public TextPosition(int lineNumber, int linePosition)
    {
        ThrowIfLessThan(lineNumber, 1);
        ThrowIfLessThan(linePosition, 1);
        
        LineNumber = lineNumber;
        LinePosition = linePosition;
    }

    public int LineNumber { get; }

    public int LinePosition { get; }

    public bool Equals(TextPosition other)
    {
        return LineNumber == other.LineNumber &&
               LinePosition == other.LinePosition;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LineNumber, LinePosition);
    }

    public static bool operator ==(TextPosition left, TextPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextPosition left, TextPosition right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{LineNumber}:{LinePosition}";
    }
}
namespace GitIStage.Text;

public readonly struct LineRange : IEquatable<LineRange>
{
    public LineRange(int lineNumber, int length)
    {
        LineNumber = lineNumber;
        Length = length;
    }

    public int LineNumber { get; }

    public int Length { get; }

    public bool Equals(LineRange other)
    {
        return LineNumber == other.LineNumber &&
               Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is LineRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LineNumber, Length);
    }

    public static bool operator ==(LineRange left, LineRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LineRange left, LineRange right)
    {
        return !left.Equals(right);
    }
}
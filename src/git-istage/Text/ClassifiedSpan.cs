namespace GitIStage.Text;

public readonly struct ClassifiedSpan : IEquatable<ClassifiedSpan>
{
    public ClassifiedSpan(TextSpan span, Classification classification)
    {
        Span = span;
        Classification = classification;
    }

    public TextSpan Span { get; }

    public Classification Classification { get; }

    public bool Equals(ClassifiedSpan other)
    {
        return Span.Equals(other.Span) &&
               Classification.Equals(other.Classification);
    }

    public override bool Equals(object? obj)
    {
        return obj is ClassifiedSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Span, Classification);
    }

    public static bool operator ==(ClassifiedSpan left, ClassifiedSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ClassifiedSpan left, ClassifiedSpan right)
    {
        return !left.Equals(right);
    }
}

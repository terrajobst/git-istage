namespace GitIStage.Text;

public readonly struct StyledSpan : IEquatable<StyledSpan>
{
    public StyledSpan(TextSpan span, TextColor? foreground, TextColor? background)
        : this(span, new TextStyle { Foreground = foreground, Background = background })
    {
    }

    public StyledSpan(TextSpan span, TextStyle style)
    {
        Span = span;
        Style = style;
    }

    public TextSpan Span { get; }

    public TextStyle Style { get; }

    public TextColor? Foreground => Style.Foreground;

    public TextColor? Background => Style.Background;

    public bool Equals(StyledSpan other)
    {
        return Span.Equals(other.Span) && Style.Equals(other.Style);
    }

    public override bool Equals(object? obj)
    {
        return obj is StyledSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Span, Style);
    }

    public static bool operator ==(StyledSpan left, StyledSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StyledSpan left, StyledSpan right)
    {
        return !left.Equals(right);
    }
}

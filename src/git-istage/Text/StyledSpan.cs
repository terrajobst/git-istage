namespace GitIStage.Text;

public readonly struct StyledSpan : IEquatable<StyledSpan>
{
    private readonly TextSpan _span;
    private readonly TextStyle _style;
    private readonly TextColor? _foreground;
    private readonly TextColor? _background;

    public StyledSpan(TextSpan span, TextColor? foreground, TextColor? background)
        : this(span, new TextStyle { Foreground = foreground, Background = background })
    {
        _span = span;
        _foreground = foreground;
        _background = background;
    }

    public StyledSpan(TextSpan span, TextStyle style)
    {
        _span = span;
        _style = style;
    }

    public TextSpan Span => _span;

    public TextStyle Style => _style;

    public TextColor? Foreground => _style.Foreground;

    public TextColor? Background => _style.Background;

    public bool Equals(StyledSpan other)
    {
        return _span.Equals(other._span) &&
               _foreground == other._foreground &&
               _background == other._background;
    }

    public override bool Equals(object? obj)
    {
        return obj is StyledSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_span, _foreground, _background);
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

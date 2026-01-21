namespace GitIStage.Text;

public readonly struct TextStyle : IEquatable<TextStyle>
{
    public TextColor? Foreground { get; init; }

    public TextColor? Background { get; init; }

    public TextAttributes Attributes { get; init; }
    
    public bool Equals(TextStyle other)
    {
        return Foreground == other.Foreground &&
               Background == other.Background &&
               Attributes == other.Attributes;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextStyle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Foreground, Background, (int)Attributes);
    }

    public static bool operator ==(TextStyle left, TextStyle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextStyle left, TextStyle right)
    {
        return !left.Equals(right);
    }

    public TextStyle PlaceOnTopOf(TextStyle previousStyle)
    {
        var foreground = Foreground is null
            ? previousStyle.Foreground
            : previousStyle.Foreground?.Combine(Foreground.Value) ?? Foreground;

        var background = Background is null
            ? previousStyle.Background
            : previousStyle.Background?.Combine(Background.Value) ?? Background;

        return new TextStyle
        {
            Foreground = foreground,
            Background = background,
            Attributes = Attributes | previousStyle.Attributes
        };
    }
}
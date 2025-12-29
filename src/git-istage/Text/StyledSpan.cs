namespace GitIStage.Text;

public readonly struct StyledSpan : IEquatable<StyledSpan>
{
    private readonly TextSpan _span;
    private readonly short _packedColor;

    // There are 16 colors, which means we need 4 bits for each color,
    // plus 1 bit whether the color is set, which means 10 bit total.
    // We store it as a single short (_packedColor) and use some bit
    // shifting to extract the data.
    //
    // Cheaper than storing 2 nullable colors, which would be 4 bytes
    // per enum + a byte for the bool, i.e. 10 bytes total.
    private const short NullFlag = 1 << (BackgroundShift - 1);
    private const short BackgroundShift = 5;
    private const short BackgroundMask = -1 << BackgroundShift;
    private const short ForegroundMask = ~BackgroundMask;

    public StyledSpan(TextSpan span, ConsoleColor? foreground, ConsoleColor? background)
    {
        _span = span;
        _packedColor = (short)(Pack(background) << BackgroundShift | (int)Pack(foreground));
    }

    public TextSpan Span => _span;

    public ConsoleColor? Foreground => Unpack(_packedColor & ForegroundMask);
    
    public ConsoleColor? Background => Unpack((_packedColor & BackgroundMask) >> BackgroundShift);

    private static short Pack(ConsoleColor? color) => (short?)color ?? NullFlag;

    private static ConsoleColor? Unpack(int value)
        => (value & NullFlag) == NullFlag ? null : (ConsoleColor)(value & ~NullFlag);

    public bool Equals(StyledSpan other)
    {
        return _span.Equals(other._span) &&
               _packedColor == other._packedColor;
    }

    public override bool Equals(object? obj)
    {
        return obj is StyledSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_span, _packedColor);
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

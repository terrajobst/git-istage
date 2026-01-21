namespace GitIStage.Text;

public readonly struct TextColor : IEquatable<TextColor>
{
    public TextColor(byte r, byte g, byte b)
        : this(r, b, b, 255)
    {
        R = r;
        G = g;
        B = b;
    }

    public TextColor(byte r, byte g, byte b, byte alpha)
    {
        R = r;
        G = g;
        B = b;
        Alpha = alpha;
    }

    public byte R { get; }

    public byte G { get; }

    public byte B { get; }

    public byte Alpha { get; }

    public TextColor WithAlpha(byte alpha)
    {
        return new TextColor(R, G, B, alpha);
    }

    public TextColor WithAlpha(float alpha)
    {
        if (alpha < 0 || alpha > 1.0)
            throw new ArgumentOutOfRangeException(nameof(alpha), alpha, null);

        var alphaByte = (byte)int.Clamp((int)(255 * alpha), 0, 255);
        return new TextColor(R, G, B, alphaByte);
    }

    public TextColor Lerp(TextColor other, float amount)
    {
        if (amount < 0 || amount > 1.0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, null);

        amount = float.Clamp(other.Alpha / 255f * amount, 0f, 1f);
        var r = (byte)double.Round(R + (other.R - R) * amount);
        var g = (byte)double.Round(G + (other.G - G) * amount);
        var b = (byte)double.Round(B + (other.B - B) * amount);
        return new TextColor(r, g, b, Alpha);
    }

    public TextColor Combine(TextColor other)
    {
        return Lerp(other, 1.0f);
    }

    public bool Equals(TextColor other)
    {
        return R == other.R &&
               G == other.G &&
               B == other.B;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextColor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B);
    }

    public static bool operator ==(TextColor left, TextColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextColor left, TextColor right)
    {
        return !left.Equals(right);
    }

    public static TextColor Black { get; } = new(12, 12, 12);
    public static TextColor DarkBlue { get; } = new(0, 55, 218);
    public static TextColor DarkGreen { get; } = new(19, 161, 14);
    public static TextColor DarkCyan { get; } = new(58, 150, 221);
    public static TextColor DarkRed { get; } = new(197, 15, 31);
    public static TextColor DarkMagenta { get; } = new(136, 23, 152);
    public static TextColor DarkYellow { get; } = new(193, 156, 0);
    public static TextColor Gray { get; } = new(204, 204, 204);
    public static TextColor DarkGray { get; } = new(118, 118, 118);
    public static TextColor Blue { get; } = new(59, 120, 255);
    public static TextColor Green { get; } = new(22, 198, 12);
    public static TextColor Cyan { get; } = new(97, 214, 214);
    public static TextColor Red { get; } = new(231, 72, 86);
    public static TextColor Magenta { get; } = new(180, 0, 158);
    public static TextColor Yellow { get; } = new(249, 241, 165);
    public static TextColor White { get; } = new(242, 242, 242);

    public static TextColor FromConsole(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => Black,
            ConsoleColor.DarkBlue => DarkBlue,
            ConsoleColor.DarkGreen => DarkGreen,
            ConsoleColor.DarkCyan => DarkCyan,
            ConsoleColor.DarkRed => DarkRed,
            ConsoleColor.DarkMagenta => DarkMagenta,
            ConsoleColor.DarkYellow => DarkYellow,
            ConsoleColor.Gray => Gray,
            ConsoleColor.DarkGray => DarkGray,
            ConsoleColor.Blue => Blue,
            ConsoleColor.Green => Green,
            ConsoleColor.Cyan => Cyan,
            ConsoleColor.Red => Red,
            ConsoleColor.Magenta => Magenta,
            ConsoleColor.Yellow => Yellow,
            ConsoleColor.White => White,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public static implicit operator TextColor(ConsoleColor color)
    {
        return FromConsole(color);
    }
}
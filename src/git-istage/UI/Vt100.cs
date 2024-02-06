namespace GitIStage.UI;

internal static class Vt100
{
    public static void SwitchToAlternateBuffer()
    {
        Console.Write("\x1b[?1049h");
    }

    public static void SwitchToMainBuffer()
    {
        Console.Write("\x1b[?1049l");
    }

    public static void ShowCursor()
    {
        Console.Write("\x1b[?25h");
    }

    public static void HideCursor()
    {
        Console.Write("\x1b[?25l");
    }

    public static void SetCursorPosition(int x, int y)
    {
        Console.Write($"\x1b[{y + 1};{x + 1}H");
    }

    public static void NegativeColors()
    {
        Console.Write("\x1b[7m");
    }

    public static void PositiveColors()
    {
        Console.Write("\x1b[27m");
    }

    public static void SetForegroundColor(ConsoleColor? color = null)
    {
        // If color is null, set to default
        var code = color != null ? GetColor(color.Value, foreground: true) : 39;
        Console.Write($"\x1b[{(int)code}m");
    }

    public static void SetBackgroundColor(ConsoleColor? color = null)
    {
        // If color is null, set to default
        var code = color != null ? GetColor(color.Value, foreground: false) : 49;
        Console.Write($"\x1b[{(int)code}m");
    }

    private static int GetColor(ConsoleColor color, bool foreground)
    {
        switch (color)
        {
            case ConsoleColor.Black when foreground: return 30;
            case ConsoleColor.DarkBlue when foreground: return 34;
            case ConsoleColor.DarkGreen when foreground: return 32;
            case ConsoleColor.DarkCyan when foreground: return 36;
            case ConsoleColor.DarkRed when foreground: return 31;
            case ConsoleColor.DarkMagenta when foreground: return 35;
            case ConsoleColor.DarkYellow when foreground: return 33;
            case ConsoleColor.Gray when foreground: return 37;
            case ConsoleColor.DarkGray when foreground: return 90;
            case ConsoleColor.Blue when foreground: return 94;
            case ConsoleColor.Green when foreground: return 92;
            case ConsoleColor.Cyan when foreground: return 96;
            case ConsoleColor.Red when foreground: return 91;
            case ConsoleColor.Magenta when foreground: return 95;
            case ConsoleColor.Yellow when foreground: return 93;
            case ConsoleColor.White when foreground: return 97;

            case ConsoleColor.Black when !foreground: return 40;
            case ConsoleColor.DarkBlue when !foreground: return 44;
            case ConsoleColor.DarkGreen when !foreground: return 42;
            case ConsoleColor.DarkCyan when !foreground: return 46;
            case ConsoleColor.DarkRed when !foreground: return 41;
            case ConsoleColor.DarkMagenta when !foreground: return 45;
            case ConsoleColor.DarkYellow when !foreground: return 43;
            case ConsoleColor.Gray when !foreground: return 47;
            case ConsoleColor.DarkGray when !foreground: return 100;
            case ConsoleColor.Blue when !foreground: return 104;
            case ConsoleColor.Green when !foreground: return 102;
            case ConsoleColor.Cyan when !foreground: return 106;
            case ConsoleColor.Red when !foreground: return 101;
            case ConsoleColor.Magenta when !foreground: return 105;
            case ConsoleColor.Yellow when !foreground: return 103;
            case ConsoleColor.White when !foreground: return 107;

            default:
                throw new Exception($"Unexpected color: {color}");
        }
    }

    public static void ResetScrollMargins()
    {
        Console.Write($"\x1b[r");
    }

    public static void SetScrollMargins(int top, int bottom)
    {
        Console.Write($"\x1b[{top};{bottom}r");
    }

    public static void ScrollUp(int lines)
    {
        Console.Write($"\x1b[{lines}S");
    }

    public static void ScrollDown(int lines)
    {
        Console.Write($"\x1b[{lines}T");
    }

    public static void EraseRestOfCurrentLine()
    {
        Console.Write($"\x1b[K");
    }
}
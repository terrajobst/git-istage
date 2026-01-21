using GitIStage.Text;

namespace GitIStage.UI;

internal static class Vt100
{
    public static void SwitchToAlternateBuffer()
    {
        Console.Write("\e[?1049h");
    }

    public static void SwitchToMainBuffer()
    {
        Console.Write("\e[?1049l");
    }

    public static void ShowCursor()
    {
        Console.Write("\e[?25h");
    }

    public static void HideCursor()
    {
        Console.Write("\e[?25l");
    }

    public static void SetCursorPosition(int x, int y)
    {
        Console.Write($"\e[{y + 1};{x + 1}H");
    }

    public static void NegativeColors()
    {
        Console.Write("\e[7m");
    }

    public static void PositiveColors()
    {
        Console.Write("\e[27m");
    }

    public static void SetForegroundColor(TextColor? color = null)
    {
        if (color is null)
            Console.Write("\e[39m");
        else
            WriteColor(color.Value, isForeground: true);
    }

    public static void SetBackgroundColor(TextColor? color = null)
    {
        if (color is null)
            Console.Write("\e[49m");
        else
            WriteColor(color.Value, isForeground: false);
    }

    public static void SetStyle(TextStyle style)
    {
        if (style.Foreground is not null)
            WriteColor(style.Foreground.Value, isForeground: true);

        if (style.Background is not null)
            WriteColor(style.Background.Value, isForeground: false);

        WriteAttributes(style.Attributes);
    }
    
    private static void WriteColor(TextColor color, bool isForeground)
    {
        var backgroundOffset = isForeground ? 0 : 10;
        var code = 30 + backgroundOffset;

        var (number, isBright) = GetStandardColor(color);
        if (number < 0)
        {
            code += 8;
            Console.Write($"\e[{code};2;{color.R};{color.G};{color.B}m");
        }
        else
        {
            code += number;
            if (isBright)
                code += 60;

            Console.Write($"\e[{code}m");
        }
    }
    
    private static void WriteAttributes(TextAttributes attributes)
    {
        if ((attributes & TextAttributes.Bold) == TextAttributes.Bold)
            Console.Write("\e[1m");
        if ((attributes & TextAttributes.Dim) == TextAttributes.Dim)
            Console.Write("\e[2m");
        if ((attributes & TextAttributes.Italic) == TextAttributes.Italic)
            Console.Write("\e[3m");
        if ((attributes & TextAttributes.Underline) == TextAttributes.Underline)
            Console.Write("\e[4m");
        if ((attributes & TextAttributes.Blink) == TextAttributes.Blink)
            Console.Write("\e[5m");
        if ((attributes & TextAttributes.Blink2) == TextAttributes.Blink2)
            Console.Write("\e[6m");
        if ((attributes & TextAttributes.Reverse) == TextAttributes.Reverse)
            Console.Write("\e[7m");
        if ((attributes & TextAttributes.Conceal) == TextAttributes.Conceal)
            Console.Write("\e[8m");
        if ((attributes & TextAttributes.Strike) == TextAttributes.Strike)
            Console.Write("\e[9m");
    }

    private static (int number, bool isBright) GetStandardColor(TextColor color)
    {
        if (color == TextColor.Black)        // Black
            return (0, false);
        if (color == TextColor.DarkRed)      // Red
            return (1, false);
        if (color == TextColor.DarkGreen)    // Green
            return (2, false);
        if (color == TextColor.DarkYellow)   // Yellow
            return (3, false);
        if (color == TextColor.DarkBlue)     // Blue
            return (4, false);
        if (color == TextColor.DarkMagenta)  // Magenta
            return (5, false);
        if (color == TextColor.DarkCyan)     // Cyan
            return (6, false);
        if (color == TextColor.Gray)         // White
            return (7, false);

        if (color == TextColor.DarkGray)    // Bright Black
            return (0, true);
        if (color == TextColor.Red)         // Bright Red
            return (1, true);
        if (color == TextColor.Green)       // Bright Green
            return (2, true);
        if (color == TextColor.Yellow)      // Bright Yellow
            return (3, true);
        if (color == TextColor.Blue)        // Bright Blue
            return (4, true);
        if (color == TextColor.Magenta)     // Bright Magenta
            return (5, true);
        if (color == TextColor.Cyan)        // Bright Cyan
            return (6, true);
        if (color == TextColor.White)       // Bright White
            return (7, true);

        return (-1, false);
    }

    public static void ResetScrollMargins()
    {
        Console.Write("\e[r");
    }

    public static void SetScrollMargins(int top, int bottom)
    {
        Console.Write($"\e[{top};{bottom}r");
    }

    public static void ScrollUp(int lines)
    {
        Console.Write($"\e[{lines}S");
    }

    public static void ScrollDown(int lines)
    {
        Console.Write($"\e[{lines}T");
    }

    public static void EraseRestOfCurrentLine()
    {
        Console.Write("\e[K");
    }
}
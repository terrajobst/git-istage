namespace GitIStage
{
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

        public static void SetForegroundColor(int r, int g, int b)
        {
            Console.Write($"\x1b[38;2;{r};{g};{b}m");
        }

        public static void SetBackgroundColor(int r, int g, int b)
        {
            Console.Write($"\x1b[48;2;{r};{g};{b}m");
        }

        public static void SetForegroundColor(ConsoleColor color)
        {
            var (r, g, b) = GetColor(color);
            SetForegroundColor(r, g, b);
        }

        public static void SetBackgroundColor(ConsoleColor color)
        {
            var (r, g, b) = GetColor(color);
            SetBackgroundColor(r, g, b);
        }

        private static (int R, int G, int B) GetColor(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return (12, 12, 12);
                case ConsoleColor.DarkBlue: return (0, 55, 218);
                case ConsoleColor.DarkGreen: return (19, 161, 14);
                case ConsoleColor.DarkCyan: return (58, 150, 221);
                case ConsoleColor.DarkRed: return (197, 15, 31);
                case ConsoleColor.DarkMagenta: return (136, 23, 152);
                case ConsoleColor.DarkYellow: return (193, 156, 0);
                case ConsoleColor.Gray: return (204, 204, 204);
                case ConsoleColor.DarkGray: return (118, 118, 118);
                case ConsoleColor.Blue: return (59, 120, 255);
                case ConsoleColor.Green: return (22, 198, 12);
                case ConsoleColor.Cyan: return (97, 214, 214);
                case ConsoleColor.Red: return (231, 72, 86);
                case ConsoleColor.Magenta: return (180, 0, 158);
                case ConsoleColor.Yellow: return (249, 241, 165);
                case ConsoleColor.White: return (242, 242, 242);
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
}
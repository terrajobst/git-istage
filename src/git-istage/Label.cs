using System;

namespace GitIStage
{
    internal sealed class Label
    {
        private readonly int _top;
        private readonly int _left;
        private readonly int _width;
        private readonly char[] _whitespace;

        private ConsoleColor _foreground = Console.ForegroundColor;
        private ConsoleColor _background = Console.BackgroundColor;
        private string _text = string.Empty;

        public Label(int top, int left, int right)
        {
            _top = top;
            _left = left;
            _width = right - left;
            _whitespace = Whitespace.GetSpaces(_width);
        }

        public ConsoleColor Foreground
        {
            get { return _foreground; }
            set
            {
                _foreground = value;
                Render();
            }
        }

        public ConsoleColor Background
        {
            get { return _background; }
            set
            {
                _background = value;
                Render();
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Render();
            }
        }

        private void Render()
        {
            var textLength = Math.Min(_text.Length, _width);
            var text = _text.Substring(0, textLength);

            var oldForeground = Console.ForegroundColor;
            var oldBackground = Console.BackgroundColor;

            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
            Console.SetCursorPosition(_left, _top);
            Console.Write(text);

            var remaining = _width - textLength;
            Console.Write(_whitespace, 0, remaining);

            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;
        }
    }
}
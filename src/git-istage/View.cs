using System;

namespace GitIStage
{
    internal sealed class View
    {
        private readonly ViewLineRenderer _renderer;
        private readonly char[] _blankRow;

        private Document _document = Document.Empty;
        private int _topLine;
        private int _leftChar;
        private int _selectedLine;
        private bool _visibleWhitespace;

        public View(ViewLineRenderer renderer, int top, int left, int bottom, int right)
        {
            _renderer = renderer;
            Top = top;
            Left = left;
            Bottom = bottom;
            Right = right;
            _blankRow = Whitespace.GetSpaces(Width);
            Initialize();
        }

        public Document Document
        {
            get { return _document; }
            set
            {
                _document = value;
                Initialize();
            }
        }

        public int SelectedLine
        {
            get { return _selectedLine; }
            set { UpdateSelectedLine(value); }
        }

        public int Top { get; }

        public int Left { get; }

        public int Bottom { get; }

        public int Right { get; }

        public int TopLine
        {
            get { return _topLine; }
            set { UpdateTopLine(value);}
        }

        public int LeftChar
        {
            get { return _leftChar; }
            set { UpdateLeftChar(value); }
        }

        public int BottomLine => TopLine + Height - 1;

        public int Height => Bottom - Top;

        public int Width => Right - Left;

        public int DocumentWidth { get; private set; }

        public int DocumentHeight => Document.Height;

        public bool VisibleWhitespace
        {
            get { return _visibleWhitespace; }
            set
            {
                if (_visibleWhitespace != value)
                {
                    _visibleWhitespace = value;
                    Render();
                }
            }
        }

        private void Initialize()
        {
            DocumentWidth = _document.Width;
            _topLine = Math.Max(0, Math.Min(Math.Min(_topLine, DocumentHeight - 1), DocumentHeight - Height));
            _selectedLine = Math.Min(_selectedLine, DocumentHeight - 1);
            _leftChar = Math.Min(_leftChar, DocumentWidth - 1);

            if (_document.Height > 0)
            {
                if (_topLine < 0)
                    _topLine = 0;

                if (_selectedLine < 0)
                    _selectedLine = 0;

                if (_leftChar < 0)
                    _leftChar = 0;
            }

            Render();
        }

        private void Render()
        {
            if (_document.Height == 0)
            {
                for (var i = Top; i < Bottom; i++)
                    RenderNonExistingLine(i);
            }
            else
            {
                var endLine = Math.Min(BottomLine, DocumentHeight - 1);

                for (var i = TopLine; i <= endLine; i++)
                    RenderLine(i);

                var remainingStart = Top + endLine - TopLine + 1;
                var remainingEnd = Top + Height;

                for (var i = remainingStart; i < remainingEnd; i++)
                    RenderNonExistingLine(i);
            }
        }

        private void RenderLine(int lineIndex)
        {
            var isVisible = TopLine <= lineIndex && lineIndex <= BottomLine;
            if (!isVisible)
                return;

            _renderer.Render(this, lineIndex);
        }

        private void RenderNonExistingLine(int visualLine)
        {
            Console.SetCursorPosition(0, visualLine);
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("~");
            Console.ForegroundColor = oldForeground;
            Console.Write(_blankRow, 0, Width - 1);
        }

        private void UpdateSelectedLine(int value)
        {
            if (_selectedLine == value)
                return;

            if (value < 0 || value >= DocumentHeight)
                throw new ArgumentOutOfRangeException(nameof(value));

            var unselectedLine = _selectedLine;
            _selectedLine = value;

            RenderLine(unselectedLine);
            RenderLine(_selectedLine);

            SelectedLineChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTopLine(int value)
        {
            if (_topLine == value)
                return;

            if (value < 0 || value >= DocumentHeight)
                throw new ArgumentOutOfRangeException(nameof(value));

            var delta = value - _topLine;
            _topLine = value;

            if (Math.Abs(delta) >= Height)
            {
                Render();
            }
            else if (delta < 0)
            {
                // We need to scroll up by -delta lines.

                var sourceVisualLine = Top;
                var targetVisualLine = Top - delta;
                var visualLineCount = Height + delta;
                Console.MoveBufferArea(Left, sourceVisualLine, Width, visualLineCount, Left, targetVisualLine);

                for (var i = 0; i < -delta; i++)
                {
                    var line = _topLine + i;
                    RenderLine(line);
                }
            }
            else
            {
                // We need to scroll down by delta lines.

                var sourceVisualLine = Top + delta;
                var targetVisualLine = Top;
                var visualLineCount = Height - delta;
                Console.MoveBufferArea(Left, sourceVisualLine, Width, visualLineCount, Left, targetVisualLine);

                for (var i = 0; i < delta; i++)
                {
                    var line = _topLine + visualLineCount + i;
                    RenderLine(line);
                }
            }

            TopLineChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateLeftChar(int value)
        {
            if (_leftChar == value)
                return;

            if (value < 0 || value >= DocumentWidth)
                throw new ArgumentOutOfRangeException(nameof(value));

            _leftChar = value;
            Render();
            LeftCharChanged?.Invoke(this, EventArgs.Empty);
        }

        public void BringIntoView(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= DocumentHeight)
                throw new ArgumentOutOfRangeException(nameof(lineIndex));

            var offScreen = lineIndex < _topLine ||
                            lineIndex > _topLine + Height - 1;
            if (!offScreen)
                return;

            var topLine = _selectedLine - Height/2;
            if (topLine < 0)
                topLine = 0;

            if (topLine > DocumentHeight - Height)
                topLine = DocumentHeight - Height;

            UpdateTopLine(topLine);
        }

        public event EventHandler SelectedLineChanged;

        public event EventHandler TopLineChanged;

        public event EventHandler LeftCharChanged;
    }
}
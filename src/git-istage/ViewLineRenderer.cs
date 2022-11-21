using System;

namespace GitIStage
{
    internal class ViewLineRenderer
    {
        public static ViewLineRenderer Default { get; } = new ViewLineRenderer();

        private readonly char[] _blankRow = Whitespace.GetSpaces(Console.WindowWidth);

        public virtual void Render(View view, int lineIndex)
        {
            var line = view.Document.GetLine(lineIndex);
            var isSelected = view.SelectedLine == lineIndex;
            var foregroundColor = ConsoleColor.Gray;
            var backgroundColor = isSelected ? ConsoleColor.DarkGray : ConsoleColor.Black;
            RenderLine(view, lineIndex, line, foregroundColor, backgroundColor);
        }

        protected void RenderLine(View view, int lineIndex, string line, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            var textStart = Math.Min(view.LeftChar, line.Length);
            var textLength = Math.Max(Math.Min(view.Width, line.Length - view.LeftChar), 0);
            var text = textLength < 0 ? string.Empty : line.ToVisual(view.VisibleWhitespace).Substring(textStart, textLength);

            var visualLine = lineIndex - view.TopLine + view.Top;

            Vt100.SetCursorPosition(view.Left, visualLine);
            Vt100.SetForegroundColor(foregroundColor);
            Vt100.SetBackgroundColor(backgroundColor);
            Vt100.EraseRestOfCurrentLine();
            Console.Write(text);

            if (view.SearchResults != null)
            {
                foreach (var hit in view.SearchResults.Hits)
                {
                    if (hit.LineIndex == lineIndex)
                    {
                        var hitStart = Math.Min(Math.Max(hit.Start - view.LeftChar, 0), view.Width - 1) + view.LeftChar;
                        var hitEnd = Math.Min(Math.Max(hit.Start + hit.Length - view.LeftChar, 0), view.Width - 1) + view.LeftChar;
                        var hitLength = hitEnd - hitStart;

                        if (hitLength > 0)
                        {
                            Vt100.SetCursorPosition(hitStart - view.LeftChar, visualLine);
                            Vt100.NegativeColors();
                            Console.Write(line.Substring(hitStart, hitLength)); // todo : 
                            Vt100.PositiveColors();
                        }
                    }
                }
            }
        }
    }
}
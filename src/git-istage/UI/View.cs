using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class View
{
    private Document _document = Document.Empty;
    private int _topLine;
    private int _leftChar;
    private Selection _selection;
    private bool _visibleWhitespace;
    private SearchResults? _searchResults;

    public View(int top, int left, int bottom, int right)
    {
        Top = top;
        Left = left;
        Bottom = bottom;
        Right = right;
        Initialize();
    }

    public Document Document
    {
        get => _document;
        set
        {
            _document = value;
            Initialize();
        }
    }

    public int SelectedLine
    {
        get => _selection.AtEnd ? _selection.EndLine : _selection.StartLine;
        set => UpdateSelection(value);
    }

    public Selection Selection
    {
        get => _selection;
        set => UpdateSelection(value);
    }

    public int Top { get; }

    public int Left { get; }

    public int Bottom { get; }

    public int Right { get; }

    public int TopLine
    {
        get => _topLine;
        set => UpdateTopLine(value);
    }

    public int LeftChar
    {
        get => _leftChar;
        set => UpdateLeftChar(value);
    }

    public int BottomLine => TopLine + Height - 1;

    public int Height => Bottom - Top;

    public int Width => Right - Left;

    public int DocumentWidth => Document.Width;

    public int DocumentHeight => Document.Height;

    public bool VisibleWhitespace
    {
        get => _visibleWhitespace;
        set
        {
            if (_visibleWhitespace != value)
            {
                _visibleWhitespace = value;
                Render();
            }
        }
    }

    public SearchResults? SearchResults
    {
        get => _searchResults;
        set
        {
            if (_searchResults != value)
            {
                _searchResults = value;
                Render();
            }
        }
    }

    private void Initialize()
    {
        // We want to preserve the current line, but we generally want to reset
        // the selection when the document changes.
        var newSelectionStart = int.Max(0, int.Min(_selection.StartLine, DocumentHeight - 1));
        var newSelectionCount = 0;
        
        _topLine = int.Max(0, int.Min(int.Min(_topLine, DocumentHeight - 1), DocumentHeight - Height));
        _selection = new Selection(newSelectionStart, newSelectionCount);
        _leftChar = int.Min(_leftChar, DocumentWidth - 1);

        if (_document.Height > 0)
        {
            if (_topLine < 0)
                _topLine = 0;

            if (_leftChar < 0)
                _leftChar = 0;
        }

        _searchResults = null;

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
            var endLine = int.Min(BottomLine, DocumentHeight - 1);

            for (var i = TopLine; i <= endLine; i++)
                RenderLine(i);

            var remainingStart = Top + endLine - TopLine + 1;
            var remainingEnd = Top + Height;

            for (var i = remainingStart; i < remainingEnd; i++)
                RenderNonExistingLine(i);
        }
    }

    // TODO: We need to properly support tabs. Right now the spans will count them as a single
    //       character which might mess up formatting.
    // TODO: We need to support rendering whitespace (tabs, spaces, line breaks,
    //       non-visible characters)
    private void RenderLine(int lineIndex)
    {
        var isVisible = TopLine <= lineIndex && lineIndex <= BottomLine;
        if (!isVisible)
            return;

        var line = Document.GetLine(lineIndex);
        var lineSpan = new TextSpan(0, line.Length);
        var clippedLineSpan = ClipSpan(lineSpan);

        var visualLine = lineIndex - TopLine + Top;
        var styledSpans = Document.GetLineStyles(lineIndex);
        var isSelected = Selection.Contains(lineIndex); 
        
        Vt100.SetCursorPosition(Left, visualLine);

        var lineBackground = (ConsoleColor?)null;

        if (isSelected)
            lineBackground = ConsoleColor.DarkBlue;

        Vt100.SetForegroundColor();
        Vt100.SetBackgroundColor(lineBackground);

        var p = clippedLineSpan.Start;
        foreach (var styledSpan in styledSpans)
        {
            var clippedSpan = ClipSpan(styledSpan);
            if (clippedSpan.Span.Length == 0)
                continue;

            if (clippedSpan.Span.Start > p)
            {
                var missingSpan = TextSpan.FromBounds(p, clippedSpan.Span.Start);
                Console.Write(line.Slice(missingSpan));
            }

            Vt100.SetForegroundColor(clippedSpan.Foreground);
            Vt100.SetBackgroundColor(lineBackground ?? clippedSpan.Background);
            Console.Write(line.Slice(clippedSpan.Span));
            
            p = clippedSpan.Span.End;
        }

        if (p < clippedLineSpan.End)
        {
            Vt100.SetForegroundColor();
            Vt100.SetBackgroundColor(lineBackground);

            var remainderSpan = TextSpan.FromBounds(p, clippedLineSpan.End);
            Console.Write(line.Slice(remainderSpan));
        }

        Vt100.EraseRestOfCurrentLine();

        RenderSearchResults(lineIndex);
    }

    private int GetVisualLine(int lineIndex)
    {
        return lineIndex - TopLine + Top;
    }

    private void RenderSearchResults(int lineIndex)
    {
        var line = Document.GetLine(lineIndex);
        var visualLine = GetVisualLine(lineIndex);
        
        if (SearchResults is not null)
        {
            foreach (var hit in SearchResults.Hits)
            {
                if (hit.LineIndex == lineIndex)
                {
                    var clippedSpan = ClipSpan(hit.Span);
                    if (clippedSpan.Length > 0)
                    {
                        Vt100.SetCursorPosition(clippedSpan.Start - LeftChar, visualLine);
                        Vt100.NegativeColors();
                        Console.Write(line.Slice(clippedSpan));
                        Vt100.PositiveColors();
                    }
                }
            }
        }
    }

    private TextSpan ClipSpan(TextSpan span)
    {
        var clippedStart = int.Clamp(span.Start, LeftChar, LeftChar + Width - 1);
        var clippedEnd = int.Clamp(span.End, LeftChar, LeftChar + Width - 1);
        return TextSpan.FromBounds(clippedStart, clippedEnd);
    }

    private StyledSpan ClipSpan(StyledSpan styledSpan)
    {
        var clippedSpan = ClipSpan(styledSpan.Span);
        return new StyledSpan(clippedSpan, styledSpan.Foreground, styledSpan.Background);
    }

    private static void RenderNonExistingLine(int visualLine)
    {
        Vt100.SetCursorPosition(0, visualLine);
        Vt100.SetForegroundColor(ConsoleColor.DarkGray);
        Vt100.SetBackgroundColor();
        Console.Write("~");
        Vt100.EraseRestOfCurrentLine();
    }

    private void UpdateSelection(int lineIndex)
    {
        var selection = new Selection(lineIndex, 0);
        UpdateSelection(selection);
    }

    private void UpdateSelection(Selection value)
    {
        if (_selection == value || DocumentHeight == 0)
            return;

        if (value.StartLine >= DocumentHeight || value.EndLine >= DocumentHeight)
            throw new ArgumentOutOfRangeException(nameof(value));

        var previousSelection = _selection;
        _selection = value;

        // Render old lines that aren't part of the new selection
        
        for (var i = previousSelection.StartLine; i <= previousSelection.EndLine; i++)
        {
            if (!value.Contains(i))
                RenderLine(i);
        }

        // Render new lines that aren't part of the old selection
        
        for (var i = value.StartLine; i <= value.EndLine; i++)
        {
            if (!previousSelection.Contains(i))
                RenderLine(i);
        }
        
        // Scroll if necessary
        
        if (value.StartLine < TopLine &&
            value.StartLine < previousSelection.StartLine)
        {
            // Start is offscreen, and we extended the start, so let's make sure the new start is visible.
            TopLine = value.StartLine;
        }
        else if (value.EndLine >= TopLine + Height &&
                 previousSelection.EndLine < value.EndLine)
        {
            // End is offscreen, and we extended the end, so let's make sure the new end is visible.
            TopLine = value.EndLine - Height + 1;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateTopLine(int value)
    {
        if (_topLine == value)
            return;

        if (value < 0 || value >= DocumentHeight)
            throw new ArgumentOutOfRangeException(nameof(value));

        var delta = value - _topLine;
        _topLine = value;

        if (int.Abs(delta) >= Height)
        {
            Render();
        }
        else
        {
            if (delta < 0)
            {
                // We need to scroll up by -delta lines.

                Vt100.SetBackgroundColor();
                Vt100.ScrollDown(int.Abs(delta));

                for (var i = 0; i < -delta; i++)
                {
                    var line = _topLine + i;
                    RenderLine(line);
                }
            }
            else
            {
                // We need to scroll down by delta lines.

                var visualLineCount = Height - delta;

                Vt100.SetBackgroundColor();
                Vt100.ScrollUp(delta);

                for (var i = 0; i < delta; i++)
                {
                    var line = _topLine + visualLineCount + i;
                    RenderLine(line);
                }
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
        if (lineIndex == -1)
            return;

        if (lineIndex < 0 || lineIndex >= DocumentHeight)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        var offScreen = lineIndex < _topLine ||
                        lineIndex > _topLine + Height - 1;
        if (!offScreen)
            return;

        var topLine = SelectedLine - Height / 2;
        if (topLine < 0)
            topLine = 0;

        if (topLine > DocumentHeight - Height)
            topLine = DocumentHeight - Height;

        UpdateTopLine(topLine);
    }

    public event EventHandler? SelectionChanged;

    public event EventHandler? TopLineChanged;

    public event EventHandler? LeftCharChanged;
}
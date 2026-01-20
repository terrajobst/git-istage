using System.Diagnostics;
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
    private bool _visible;

    public bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            if (_visible)
                Render();
        }
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

    public int Top { get; private set; }

    public int Left { get; private set;  }

    public int Bottom { get; private set;  }

    public int Right { get; private set;  }

    public int TopLine
    {
        get => _topLine;
        set => UpdateTopLine(value);
    }

    public int TopLineMax => int.Max(0, _document.Height - 1);

    public int LeftChar
    {
        get => _leftChar;
        set => UpdateLeftChar(value);
    }

    public int LeftCharMax => int.Max(0, _document.Width - 1);

    public int BottomLine => TopLine + Height - 1;

    public int Height => Bottom - Top;

    public int Width => Right - Left;

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
    
    public void Resize(int top, int left, int bottom, int right)
    {
        Top = top;
        Left = left;
        Bottom = bottom;
        Right = right;
        Initialize();
    }

    private void Initialize()
    {
        // We want to preserve the current line, but we generally want to reset
        // the selection when the document changes.
        var newSelectionStart = int.Max(0, int.Min(_selection.StartLine, Document.Height - 1));
        var newSelectionCount = 0;

        _topLine = int.Clamp(_topLine, 0, TopLineMax); 
        _selection = new Selection(newSelectionStart, newSelectionCount);
        _leftChar = int.Clamp(_leftChar, 0, LeftCharMax);
        _searchResults = null;

        Render();
    }

    private void Render()
    {
        for (var i = TopLine; i <= BottomLine; i++)
            RenderLine(i);
    }

    // TODO: We need to properly support tabs. Right now the spans will count them as a single
    //       character which might mess up formatting.
    // TODO: We need to support rendering whitespace (tabs, spaces, line breaks,
    //       non-visible characters)
    private void RenderLine(int lineIndex)
    {
        if (!IsVisible(lineIndex))
            return;

        if (lineIndex >= Document.Height)
        {
            RenderNonExistingLine(lineIndex);
            return;
        }

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

    private void RenderNonExistingLine(int lineIndex)
    {
        Debug.Assert(IsVisible(lineIndex));

        var visualLine = GetVisualLine(lineIndex); 
        
        Vt100.SetCursorPosition(0, visualLine);
        Vt100.SetForegroundColor(ConsoleColor.DarkGray);
        Vt100.SetBackgroundColor();
        Console.Write("~");
        Vt100.EraseRestOfCurrentLine();
    }

    private void RenderSearchResults(int lineIndex)
    {
        Debug.Assert(IsVisible(lineIndex));

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

    private bool IsVisible(int lineIndex)
    {
        return Visible && TopLine <= lineIndex && lineIndex <= BottomLine;
    }

    private int GetVisualLine(int lineIndex)
    {
        Debug.Assert(IsVisible(lineIndex));

        return lineIndex - TopLine + Top;
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

    private void UpdateSelection(int lineIndex)
    {
        var selection = new Selection(lineIndex, 0);
        UpdateSelection(selection);
    }

    private void UpdateSelection(Selection value)
    {
        if (_selection == value || Document.Height == 0)
            return;

        ThrowIfGreaterThanOrEqual(value.StartLine, Document.Height);
        ThrowIfGreaterThanOrEqual(value.EndLine, Document.Height);

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

    private void UpdateTopLine(int lineIndex)
    {
        if (_topLine == lineIndex || Document.Height == 0)
            return;

        ThrowIfLessThan(lineIndex, 0);
        ThrowIfGreaterThanOrEqual(lineIndex, TopLineMax);

        var delta = lineIndex - _topLine;
        _topLine = lineIndex;

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

    private void UpdateLeftChar(int charIndex)
    {
        if (_leftChar == charIndex ||  Document.Height == 0)
            return;

        ThrowIfLessThan(charIndex, 0);
        ThrowIfGreaterThanOrEqual(charIndex, LeftCharMax);

        _leftChar = charIndex;
        Render();
        LeftCharChanged?.Invoke(this, EventArgs.Empty);
    }

    public void BringIntoView(int lineIndex)
    {
        if (Document.Height == 0)
            return;

        ThrowIfLessThan(lineIndex, 0);
        ThrowIfGreaterThanOrEqual(lineIndex, Document.Height);

        var offScreen = lineIndex < _topLine ||
                        lineIndex > _topLine + Height - 1;
        if (!offScreen)
            return;

        var topLine = SelectedLine - Height / 2;
        if (topLine < 0)
            topLine = 0;

        if (topLine > Document.Height - Height)
            topLine = Document.Height - Height;

        UpdateTopLine(topLine);
    }

    public event EventHandler? SelectionChanged;

    public event EventHandler? TopLineChanged;

    public event EventHandler? LeftCharChanged;
}
using System.Diagnostics;
using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class View
{
    private readonly ThemeService _themeService;
    private Document _document = Document.Empty;
    private int _topLine;
    private int _leftChar;
    private Selection _selection;
    private SearchResults? _searchResults;
    private readonly List<ClassifiedSpan> _lineStyles = new();

    public View(ThemeService themeService)
    {
        _themeService = themeService;
    }

    public bool Visible { get; set; }

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
        set => SetSelection(new Selection(value, 0));
    }

    public Selection Selection
    {
        get => _selection;
        set => SetSelection(value);
    }

    public int Top { get; private set; }

    public int Left { get; private set; }

    public int Bottom { get; private set; }

    public int Right { get; private set; }

    public int TopLine
    {
        get => _topLine;
        set => SetTopLine(value);
    }

    public int TopLineMax => int.Max(0, _document.Height - 1);

    public int LeftChar
    {
        get => _leftChar;
        set => SetLeftChar(value);
    }

    public int LeftCharMax => int.Max(0, _document.Width - 1);

    public int BottomLine => TopLine + Height - 1;

    public int Height => Bottom - Top;

    public int Width => Right - Left;

    public bool VisibleWhitespace { get; set; }

    public SearchResults? SearchResults
    {
        get => _searchResults;
        set => _searchResults = value;
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
    }

    public void Render(RenderBuffer buffer)
    {
        for (var i = TopLine; i <= BottomLine; i++)
            RenderLine(buffer, i);
    }

    // TODO: We need to properly support tabs. Right now the spans will count them as a single
    //       character which might mess up formatting.
    // TODO: We need to support rendering whitespace (tabs, spaces, line breaks,
    //       non-visible characters)
    private void RenderLine(RenderBuffer buffer, int lineIndex)
    {
        if (!IsVisible(lineIndex))
            return;

        if (lineIndex >= Document.Height)
        {
            RenderNonExistingLine(buffer, lineIndex);
            return;
        }

        var line = Document.GetLine(lineIndex);
        var lineSpan = new TextSpan(0, line.Length);
        var clippedLineSpan = ClipSpan(lineSpan);

        var visualLine = lineIndex - TopLine + Top;
        var isSelected = Selection.Contains(lineIndex);

        _lineStyles.Clear();
        Document.GetLineStyles(lineIndex, _lineStyles);

        // Extract line-level style encoded as a zero-length sentinel span at the start
        TextStyle lineStyle = default;
        if (_lineStyles.Count > 0 && _lineStyles[0].Span.Length == 0)
        {
            lineStyle = _themeService.ResolveStyle(_lineStyles[0].Classification);
            _lineStyles.RemoveAt(0);
        }

        buffer.SetCursorPosition(Left, visualLine);

        if (isSelected)
        {
            lineStyle = new TextStyle()
            {
                Foreground = lineStyle.Foreground,
                Background = lineStyle.Background?.Combine(_themeService.Colors.Selection) ?? _themeService.Colors.Selection
            };
        }

        buffer.SetForegroundColor(lineStyle.Foreground);
        buffer.SetBackgroundColor(lineStyle.Background);

        var p = clippedLineSpan.Start;
        foreach (var classifiedSpan in _lineStyles)
        {
            var clippedSpan = ClipSpan(classifiedSpan);
            if (clippedSpan.Span.Length == 0)
                continue;

            if (clippedSpan.Span.Start > p)
            {
                var missingSpan = TextSpan.FromBounds(p, clippedSpan.Span.Start);
                buffer.Write(line.Slice(missingSpan));
            }

            var style = ResolveStyle(clippedSpan, lineStyle);

            buffer.SetForegroundColor(style.Foreground);
            buffer.SetBackgroundColor(style.Background);
            buffer.Write(line.Slice(clippedSpan.Span));

            p = clippedSpan.Span.End;
        }

        if (p < clippedLineSpan.End)
        {
            buffer.SetForegroundColor(lineStyle.Foreground);
            buffer.SetBackgroundColor(lineStyle.Background);

            var remainderSpan = TextSpan.FromBounds(p, clippedLineSpan.End);
            buffer.Write(line.Slice(remainderSpan));
        }

        buffer.EraseRestOfCurrentLine();

        RenderSearchResults(buffer, lineIndex);
    }

    private TextStyle ResolveStyle(ClassifiedSpan span, TextStyle lineStyle)
    {
        var style = _themeService.ResolveStyle(span.Classification);

        if (_themeService.IsKnownClassification(span.Classification))
            return style.PlaceOnTopOf(lineStyle);

        // Syntax tokens get background from line but NOT foreground,
        // so they don't inherit added/deleted line colors.
        return new TextStyle
        {
            Foreground = style.Foreground,
            Background = style.Background ?? lineStyle.Background,
            Attributes = style.Attributes | lineStyle.Attributes
        };
    }

    private void RenderNonExistingLine(RenderBuffer buffer, int lineIndex)
    {
        Debug.Assert(IsVisible(lineIndex));

        var visualLine = GetVisualLine(lineIndex);

        buffer.SetCursorPosition(0, visualLine);
        buffer.SetForegroundColor(_themeService.Colors.NonExistingTextForeground);
        buffer.SetBackgroundColor(_themeService.Colors.NonExistingTextBackground);
        buffer.Write('~');
        buffer.EraseRestOfCurrentLine();
    }

    private void RenderSearchResults(RenderBuffer buffer, int lineIndex)
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
                        buffer.SetCursorPosition(clippedSpan.Start - LeftChar, visualLine);
                        buffer.NegativeColors();
                        buffer.Write(line.Slice(clippedSpan));
                        buffer.PositiveColors();
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

    private ClassifiedSpan ClipSpan(ClassifiedSpan classifiedSpan)
    {
        var clippedSpan = ClipSpan(classifiedSpan.Span);
        return new ClassifiedSpan(clippedSpan, classifiedSpan.Classification);
    }

    private void SetSelection(Selection value)
    {
        if (_selection == value || Document.Height == 0)
            return;

        ThrowIfGreaterThanOrEqual(value.StartLine, Document.Height);
        ThrowIfGreaterThanOrEqual(value.EndLine, Document.Height);

        _selection = value;

        BringIntoView(SelectedLine);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetTopLine(int lineIndex)
    {
        if (_topLine == lineIndex || Document.Height == 0)
            return;

        ThrowIfLessThan(lineIndex, 0);
        ThrowIfGreaterThanOrEqual(lineIndex, TopLineMax);

        _topLine = lineIndex;

        TopLineChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetLeftChar(int charIndex)
    {
        if (_leftChar == charIndex || Document.Height == 0)
            return;

        ThrowIfLessThan(charIndex, 0);
        ThrowIfGreaterThanOrEqual(charIndex, LeftCharMax);

        _leftChar = charIndex;
        LeftCharChanged?.Invoke(this, EventArgs.Empty);
    }

    public void BringIntoView(int lineIndex)
    {
        if (Document.Height == 0)
            return;

        ThrowIfLessThan(lineIndex, 0);
        ThrowIfGreaterThanOrEqual(lineIndex, Document.Height);

        if (lineIndex < _topLine)
            SetTopLine(lineIndex);
        else if (lineIndex > _topLine + Height - 1)
            SetTopLine(int.Min(lineIndex - Height + 1, TopLineMax));
    }

    public event EventHandler? SelectionChanged;

    public event EventHandler? TopLineChanged;

    public event EventHandler? LeftCharChanged;
}

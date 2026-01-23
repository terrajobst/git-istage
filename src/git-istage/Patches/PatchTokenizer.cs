using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using GitIStage.Text;

namespace GitIStage.Patches;

internal sealed class PatchTokenizer
{
    private struct TokenData
    {
        public TokenData(PatchNodeKind kind, TextSpan span, object? value)
        {
            Span = span;
            Kind = kind;
            Value = value;
        }

        public PatchNodeKind Kind  { get; }

        public TextSpan Span { get; }

        public object? Value { get; }

        public PatchTrivia? TrailingTrivia
        {
            get;
            set
            {
                Debug.Assert(TrailingTrivia is null);
                field = value;
            }
        }
    }

    private readonly Patch _root;
    private readonly SourceText _text;
    private readonly List<TokenData> _tokens = new List<TokenData>();

    private TextLine? _currentLine;
    private int _offset;

    public PatchTokenizer(Patch root, SourceText text)
    {
        _root = root;
        _text = text;
        if (_text.Lines.Any())
            _currentLine = text.Lines[0];
    }

    public Patch Root => _root;

    public SourceText Text => _text;

    public TextLine? CurrentLine => _currentLine;

    public bool IsEndOfLine()
    {
        return _currentLine is null ||
               _offset >= _currentLine.End;
    }

    public bool IsEndOfFile()
    {
        return _offset >= _text.Length;
    }

    public char GetCurrentChar()
    {
        return _currentLine is not null && _offset < _currentLine.SpanIncludingLineBreak.End
            ? _text[_offset]
            : '\0';
    }

    public bool StartsWith(ReadOnlySpan<char> text)
    {
        if (IsEndOfFile() || _currentLine is null)
            return false;

        if (!_currentLine.SpanIncludingLineBreak.Contains(_offset))
            Debugger.Break();

        if (!_currentLine.Span.Contains(_offset))
            Debugger.Break();

        var remainderSpan = TextSpan.FromBounds(_offset, _currentLine.End);
        var remainder = _text.AsSpan(remainderSpan);
        return remainder.StartsWith(text);
    }

    public void NextLine()
    {
        if (_currentLine is null)
            return;

        if (_offset < _currentLine.End)
            throw ExpectedTextAt(_offset, "<end-of-line>");

        _offset = _currentLine.SpanIncludingLineBreak.End;

        var index = _text.GetLineIndex(_offset);
        _currentLine = _text.Lines[index];

        if (!IsEndOfFile() && _offset != _currentLine.Start)
            Debugger.Break();

        Debug.Assert(IsEndOfFile() || _offset == _currentLine.Start);

        _tokens.Clear();
    }

    public PatchTokenHandle FabricateToken(PatchNodeKind tokenKind)
    {
        var tokenSpan = new TextSpan(_offset, 0);
        return RecordToken(tokenKind, tokenSpan);
    }

    public PatchTokenHandle<T> FabricateToken<T>(PatchNodeKind tokenKind, T tokenValue)
    {
        var tokenSpan = new TextSpan(_offset, 0);
        return RecordToken(tokenKind, tokenSpan, tokenValue);
    }

    public void ParseSpace()
    {
        SetTriviaOfLastToken(ReadSpaceTrivia());
    }

    public void ParseEndOfLine()
    {
        SetTriviaOfLastToken(ReadEndOfLineTrivia());
    }

    private void SetTriviaOfLastToken(PatchTrivia trivia)
    {
        Debug.Assert(_tokens.Count > 0);

        // Since TokenData is a struct we need to use
        // this construct to avoid assigning to a copy.
        var span = CollectionsMarshal.AsSpan(_tokens);
        ref var token = ref span[^1];
        token.TrailingTrivia = trivia;
    }

    private PatchTrivia ReadSpaceTrivia()
    {
        if (GetCurrentChar() != ' ')
            throw ExpectedTextAt(_offset, "<space>");

        var start = _offset;
        _offset++;
        var end = _offset;

        var triviaKind = PatchNodeKind.SpaceTrivia;
        var triviaSpan = TextSpan.FromBounds(start, end);

        return new PatchTrivia(_root, triviaKind, triviaSpan);
    }

    private PatchTrivia ReadEndOfLineTrivia()
    {
        if (_currentLine is null || _offset != _currentLine.SpanLineBreak.Start)
            throw ExpectedTextAt(_offset, "<end-of-line>");

        _offset += _currentLine.SpanLineBreak.Length;

        var triviaKind = PatchNodeKind.EndOfLineTrivia;
        var triviaSpan = _currentLine.SpanLineBreak;

        return new PatchTrivia(_root, triviaKind, triviaSpan);
    }

    public PatchTokenHandle ParseToken(PatchNodeKind tokenKind)
    {
        var text = tokenKind.GetTokenText();
        Debug.Assert(text is not null);
        return ParseToken(tokenKind, text);
    }

    private PatchTokenHandle ParseToken(PatchNodeKind kind, ReadOnlySpan<char> text)
    {
        EnsureHasRemainingText(text);

        var start = _offset;
        var end = int.Min(_offset + text.Length, _text.Length);
        var tokenSpan = TextSpan.FromBounds(start, end);

        var tokenText = _text.AsSpan(tokenSpan);

        if (!tokenText.SequenceEqual(text))
            throw ExpectedTextAt(start, text);

        return RecordToken(kind, tokenSpan);
    }

    public PatchTokenHandle<string> ParsePath()
    {
        return ParseTokenUntilEndOfLineAsString(PatchNodeKind.PathToken, "<path>");
    }

    public PatchTokenHandle<string> ParsePath(ReadOnlySpan<char> requiredPrefix)
    {
        EnsureHasRemainingText("<path>");

        var start = _offset;
        var end = _currentLine.End;
        var tokenSpan = TextSpan.FromBounds(start, end);
        var tokenText = _text.AsSpan(tokenSpan);
        var isDevNull = tokenText is "/dev/null";

        if (!isDevNull && !tokenText.StartsWith(requiredPrefix))
            throw ExpectedTextAt(start, requiredPrefix);

        var tokenValue = isDevNull ? "" : tokenText.Slice(requiredPrefix.Length).ToString();

        return RecordToken(PatchNodeKind.PathToken, tokenSpan, tokenValue);
    }

    public PatchTokenHandle<string> ParsePathUntil(ReadOnlySpan<char> requiredPrefix, ReadOnlySpan<char> text)
    {
        EnsureHasRemainingText("<path>");

        var remainderSpan = TextSpan.FromBounds(_offset, _currentLine.End);
        var remainder = _text.AsSpan(remainderSpan);
        var indexOfText = remainder.IndexOf(text);
        var until = indexOfText < 0 ? _currentLine.End : _offset + indexOfText;

        var start = _offset;
        var end = until;
        var tokenSpan = TextSpan.FromBounds(start, end);
        var tokenText = _text.AsSpan(tokenSpan);
        var isDevNull = tokenText is "/dev/null";

        if (!isDevNull && !tokenText.StartsWith(requiredPrefix))
            throw ExpectedTextAt(start, requiredPrefix);

        var tokenValue = isDevNull ? "" : tokenText.Slice(requiredPrefix.Length).ToString();

        return RecordToken(PatchNodeKind.PathToken, tokenSpan, tokenValue);
    }

    public PatchTokenHandle<string> ParseText()
    {
        return ParseTokenUntilEndOfLineAsString(PatchNodeKind.TextToken, "<text>");
    }

    public PatchTokenHandle<string> ParseTextOrEmpty()
    {
        if (_currentLine is null || IsEndOfLine() || IsEndOfFile())
            return FabricateToken(PatchNodeKind.TextToken, "");

        return ParseTokenUntilEndOfLineAsString(PatchNodeKind.TextToken, "<text>");
    }

    private PatchTokenHandle<string> ParseTokenUntilEndOfLineAsString(PatchNodeKind tokenKind, ReadOnlySpan<char> text)
    {
        EnsureHasRemainingText(text);

        var start = _offset;
        var end = _currentLine.End;
        var tokenSpan = TextSpan.FromBounds(start, end);
        var tokenValue = _text.ToString(tokenSpan);

        return RecordToken(tokenKind, tokenSpan, tokenValue);
    }

    public PatchTokenHandle<string> ParseHash()
    {
        EnsureHasRemainingText("<hash>");

        var start = _offset;

        while (GetCurrentChar() is >= '0' and <= '9' or >= 'a' and <= 'f')
            _offset++;

        var end = _offset;
        var tokenSpan = TextSpan.FromBounds(start, end);

        if (tokenSpan.Length == 0)
            throw ExpectedTextAt(start, "<hash>");

        var tokenValue = _text.ToString(tokenSpan);

        return RecordToken(PatchNodeKind.HashToken, tokenSpan, tokenValue);
    }

    public PatchTokenHandle<PatchEntryMode> ParseMode()
    {
        EnsureHasRemainingText("<mode>");

        var start = _offset;

        while (GetCurrentChar() is >= '0' and <= '7')
            _offset++;

        var end = _offset;
        var tokenSpan = TextSpan.FromBounds(start, end);
        var tokenText = _text.AsSpan(tokenSpan);

        if (!TryParseOctal(tokenText, out var value))
            throw ExpectedTextAt(start, "<mode>");

        var tokenValue = GetMode(start, value);

        return RecordToken(PatchNodeKind.ModeToken, tokenSpan, tokenValue);

        static bool TryParseOctal(ReadOnlySpan<char> text, out int value)
        {
            if (text.Length is 0 or > 6)
            {
                value = 0;
                return false;
            }

            var result = 0;

            foreach (var c in text)
            {
                if (c is < '0' or > '7')
                {
                    value = 0;
                    return false;
                }

                var d = c - '0';
                result = result * 8 + d;
            }

            value = result;
            return true;
        }
    }

    private PatchEntryMode GetMode(int start, int value)
    {
        switch (value)
        {
            case 0x0000:
                return PatchEntryMode.Nonexistent;
            case 0x4000:
                return PatchEntryMode.Directory;
            case 0x81A4:
                return PatchEntryMode.RegularNonExecutableFile;
            case 0x81B4:
                return PatchEntryMode.RegularNonExecutableGroupWriteableFile;
            case 0x81ED:
                return PatchEntryMode.RegularExecutableFile;
            case 0xA000:
                return PatchEntryMode.SymbolicLink;
            case 0xE000:
                return PatchEntryMode.Gitlink;
            default:
                throw ExpectedTextAt(start, "<mode>");
        }
    }

    public PatchTokenHandle<float> ParsePercentage()
    {
        EnsureHasRemainingText("<percentage>");

        var start = _offset;

        while (GetCurrentChar() is >= '0' and <= '9')
            _offset++;

        var numberEnd = _offset;
        var numberSpan = TextSpan.FromBounds(start, numberEnd);
        var numberText = _text.AsSpan(numberSpan);

        if (!int.TryParse(numberText, out var percent) || percent < 0 || percent > 100)
            throw ExpectedTextAt(start, "<percentage>");

        if (GetCurrentChar() == '%')
            _offset++;
        else
            throw ExpectedTextAt(_offset, "%");

        var tokenEnd = _offset;
        var tokenSpan = TextSpan.FromBounds(start, tokenEnd);
        var tokenValue = percent / 100f;

        return RecordToken(PatchNodeKind.PercentageToken, tokenSpan, tokenValue);
    }

    public PatchTokenHandle<LineRange> ParseRange()
    {
        EnsureHasRemainingText("<range>");

        var start = _offset;
        var lineNumber = ReadInteger();
        var lineCount = 1;

        if (GetCurrentChar() == ',')
        {
            _offset++;
            lineCount = ReadInteger();
        }

        var tokenSpan = TextSpan.FromBounds(start, _offset);
        var tokenValue = new LineRange(lineNumber, lineCount);

        return RecordToken(PatchNodeKind.RangeToken, tokenSpan, tokenValue);
    }

    private int ReadInteger()
    {
        var start = _offset;

        while (GetCurrentChar() is >= '0' and <= '9')
            _offset++;

        var end = _offset;

        var span = TextSpan.FromBounds(start, end);
        var text = _text.AsSpan(span);

        if (!int.TryParse(text, out var lineNumber))
            throw ExpectedTextAt(start, "<integer>");

        return lineNumber;
    }

    private PatchTokenHandle RecordToken(PatchNodeKind tokenKind, TextSpan tokenSpan)
    {
        var tokenIndex = RecordToken(tokenKind, tokenSpan, null);
        return new PatchTokenHandle(this, tokenIndex);
    }

    private PatchTokenHandle<T> RecordToken<T>(PatchNodeKind tokenKind, TextSpan tokenSpan, T tokenValue)
    {
        var tokenIndex = RecordToken(tokenKind, tokenSpan, (object?)tokenValue);
        return new PatchTokenHandle<T>(this, tokenIndex);
    }

    private int RecordToken(PatchNodeKind tokenKind, TextSpan tokenSpan, object? tokenValue)
    {
        _offset = tokenSpan.End;

        var tokenIndex = _tokens.Count;

        _tokens.Add(new TokenData(tokenKind, tokenSpan, tokenValue));
        return tokenIndex;
    }

    public PatchToken CreateToken(int tokenIndex)
    {
        var tokenData = _tokens[tokenIndex];
        return PatchToken.Create(_root, tokenData.Kind, tokenData.Span, tokenData.TrailingTrivia);
    }

    public PatchToken<T> CreateToken<T>(int tokenIndex)
    {
        var tokenData = _tokens[tokenIndex];
        return PatchToken.Create(_root, tokenData.Kind, tokenData.Span, (T)tokenData.Value!, tokenData.TrailingTrivia);
    }

    [MemberNotNull(nameof(_currentLine))]
    private void EnsureHasRemainingText(ReadOnlySpan<char> expected)
    {
        if (_currentLine is null || IsEndOfLine() || IsEndOfFile())
            throw ExpectedTextAt(_offset, expected);
    }

    private FormatException ExpectedTextAt(int offset, ReadOnlySpan<char> expectedText)
    {
        var position = _text.GetPosition(offset);
        return new FormatException($"Invalid patch. Expected '{expectedText}' at {position}");
    }
}
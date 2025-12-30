using System.Collections.Immutable;

namespace GitIStage.Text;

public sealed class SourceText
{
    public static SourceText Empty { get; } = new(string.Empty, "");

    private readonly string _text;

    private SourceText(string text, string fileName)
    {
        ThrowIfNull(text);
        ThrowIfNull(fileName);

        _text = text;
        FileName = fileName;
        Lines = ParseLines(this, text);
    }

    public static SourceText From(string text, string fileName = "")
    {
        ThrowIfNull(text);
        ThrowIfNull(fileName);

        return new SourceText(text, fileName);
    }

    private static ImmutableArray<TextLine> ParseLines(SourceText sourceText, string text)
    {
        ThrowIfNull(sourceText);
        ThrowIfNull(text);

        if (text.Length == 0)
            return ImmutableArray<TextLine>.Empty;

        var result = ImmutableArray.CreateBuilder<TextLine>();

        var position = 0;
        var lineStart = 0;

        while (position < text.Length)
        {
            var lineBreakWidth = GetLineBreakWidth(text, position);

            if (lineBreakWidth == 0)
            {
                position++;
            }
            else
            {
                AddLine(result, sourceText, position, lineStart, lineBreakWidth);

                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (result.Count == 0 || position > lineStart)
            AddLine(result, sourceText, position, lineStart, 0);

        return result.ToImmutable();
    }

    private static void AddLine(ImmutableArray<TextLine>.Builder result, SourceText sourceText, int position, int lineStart, int lineBreakWidth)
    {
        var lineLength = position - lineStart;
        var lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
        var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
        result.Add(line);
    }

    private static int GetLineBreakWidth(string text, int position)
    {
        var c = text[position];
        var l = position + 1 >= text.Length ? '\0' : text[position + 1];

        if (c == '\r' && l == '\n')
            return 2;

        if (c == '\r' || c == '\n')
            return 1;

        return 0;
    }

    public ImmutableArray<TextLine> Lines { get; }

    public char this[int index] => _text[index];

    public int Length => _text.Length;

    public string FileName { get; }

    public int GetLineIndex(int position)
    {
        ThrowIfNegative(position);

        var lower = 0;
        var upper = Lines.Length - 1;

        while (lower <= upper)
        {
            var index = lower + (upper - lower) / 2;
            var start = Lines[index].Start;

            if (position == start)
                return index;

            if (start > position)
            {
                upper = index - 1;
            }
            else
            {
                lower = index + 1;
            }
        }

        return lower - 1;
    }

    public ReadOnlySpan<char> AsSpan() => _text.AsSpan();

    public ReadOnlySpan<char> AsSpan(TextSpan span) => _text.AsSpan(span);

    public override string ToString() => _text;

    public string ToString(int start, int length) => _text.Substring(start, length);

    public string ToString(TextSpan span) => ToString(span.Start, span.Length);
}
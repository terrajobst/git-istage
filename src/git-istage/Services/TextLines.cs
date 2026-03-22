using System.Collections;
using System.Collections.Immutable;
using GitIStage.Patches;
using GitIStage.Text;

namespace GitIStage.Services;

public sealed class TextLines : IEnumerable<ReadOnlyMemory<char>>
{
    public static TextLines Empty { get; } = new(ImmutableArray<ReadOnlyMemory<char>>.Empty);

    private readonly ImmutableArray<ReadOnlyMemory<char>> _lines;

    private TextLines(ImmutableArray<ReadOnlyMemory<char>> lines)
    {
        _lines = lines;
    }

    public static TextLines FromFile(string fileName)
    {
        if (!File.Exists(fileName))
            return Empty;

        var lines = File.ReadLines(fileName)
                        .Select(l => l.AsMemory())
                        .ToImmutableArray();
        return new TextLines(lines);
    }

    public static TextLines FromStream(Stream stream)
    {
        var lines = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
            lines.Add(line.AsMemory());

        return new TextLines(lines.ToImmutable());
    }

    public ReadOnlyMemory<char> this[int index] => _lines[index];

    public int Count => _lines.Length;

    public TextLines ApplyReversed(PatchEntry? patchEntry)
    {
        if (patchEntry is null)
            return this;

        var result = new List<ReadOnlyMemory<char>>();
        var text = patchEntry.Root.Text;
        var lastLine = 0;

        foreach (var hunk in patchEntry.Hunks)
        {
            var startLine = hunk.NewRange.LineNumber - 1;
            CopyLinesTo(result, lastLine, startLine);

            foreach (var line in hunk.Lines)
            {
                if (line.Kind is PatchNodeKind.ContextLine or
                                 PatchNodeKind.DeletedLine)
                {
                    var lineSpan = TextSpan.FromBounds(line.Span.Start + 1, line.Span.End);
                    var lineText = text.AsMemory(lineSpan);
                    result.Add(lineText);
                }
            }

            lastLine = hunk.NewRange.LineNumber - 1 + hunk.NewRange.Length;
        }

        CopyLinesTo(result, lastLine, _lines.Length);

        return new TextLines([.. result]);
    }

    private void CopyLinesTo(List<ReadOnlyMemory<char>> target, int startLine, int endLine)
    {
        if (startLine < 0 || endLine < 0)
            return;

        var span = _lines.AsSpan(startLine, endLine - startLine);
        target.AddRange(span);
    }

    public IEnumerator<ReadOnlyMemory<char>> GetEnumerator()
    {
        return ((IEnumerable<ReadOnlyMemory<char>>)_lines).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

#if DEBUG
    public bool SequenceEqual(TextLines other)
    {
        return _lines.SequenceEqual(other._lines, (x, y) => x.Span.SequenceEqual(y.Span));
    }
#endif
}

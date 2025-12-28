using GitIStage.UI;
using LibGit2Sharp;

namespace GitIStage.Patches;

internal sealed class PatchDocument : Document
{
    public PatchDocument(IReadOnlyList<PatchEntry> entries, IReadOnlyList<PatchLine> lines, bool isStaged)
    {
        Entries = entries;
        Lines = lines;
        IsStaged = isStaged;
        Width = lines.Select(l => l.Text).DefaultIfEmpty(string.Empty).Max(t => t.LengthVisual());
    }

    public IReadOnlyList<PatchEntry> Entries { get; }

    public IReadOnlyList<PatchLine> Lines { get; }

    public bool IsStaged { get; }

    public override int Height => Lines.Count;

    public override int Width { get; }

    public override int EntryCount => Entries.Count;

    public override string GetLine(int index)
    {
        return Lines[index].Text;
    }

    public override int GetLineIndex(int index)
    {
        return Entries[index].Offset;
    }

    public PatchEntry? FindEntry(int lineIndex)
    {
        var index = FindEntryIndex(lineIndex);
        return index < 0 ? null : Entries[index];
    }

    public override int FindEntryIndex(int lineIndex)
    {
        // TODO: binary search would be more appropriate

        for (var i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            if (e.Offset <= lineIndex && lineIndex < e.Offset + e.Length)
                return i;
        }

        return -1;
    }

    public static PatchDocument Parse(Patch? patch, bool isStaged)
    {
        if (patch is null)
            return new PatchDocument(Array.Empty<PatchEntry>(), Array.Empty<PatchLine>(), isStaged);

        var lines = new List<PatchLine>();
        var entries = new List<PatchEntry>();

        foreach (var change in patch)
        {
            var changeLines = ParseLines(change.Patch);
            var entryOffset = lines.Count;
            var entryLength = changeLines.Count;
            lines.AddRange(changeLines);

            var hunks = new List<PatchHunk>();

            var hunkOffset = GetNextHunk(changeLines, -1);
            while (hunkOffset < changeLines.Count)
            {
                var hunkEnd = GetNextHunk(changeLines, hunkOffset) - 1;
                var hunkLength = hunkEnd - hunkOffset + 1;
                var hunkLine = changeLines[hunkOffset].Text;

                if (TryGetHunkInformation(hunkLine, out var oldStart, out var oldLength, out var newStart, out var newLength))
                {
                    var hunk = new PatchHunk(entryOffset + hunkOffset, hunkLength, oldStart, oldLength, newStart, newLength);
                    hunks.Add(hunk);
                }

                hunkOffset = hunkEnd + 1;
            }

            var entry = new PatchEntry(entryOffset, entryLength, change.OldPath, (int)change.OldMode, change.Path, (int)change.Mode, hunks);
            entries.Add(entry);
        }

        return new PatchDocument(entries, lines, isStaged);
    }

    private static int GetNextHunk(IReadOnlyList<PatchLine> lines, int index)
    {
        index++;

        while (index < lines.Count && lines[index].Kind != PatchLineKind.Hunk)
            index++;

        return index;
    }

    private static bool TryGetHunkInformation(string hunkLine,
        out int oldStart,
        out int oldLength,
        out int newStart,
        out int newLength)
    {
        oldStart = 0;
        oldLength = 0;
        newStart = 0;
        newLength = 0;

        if (!hunkLine.StartsWith("@@"))
            return false;

        var hunkMarkerEnd = hunkLine.IndexOf("@@", 2, StringComparison.Ordinal);
        if (hunkMarkerEnd < 0)
            return false;

        var rangeInformation = hunkLine.Substring(2, hunkMarkerEnd - 2).Trim();
        var ranges = rangeInformation.Split(' ');
        if (ranges.Length != 2)
            return false;

        if (!TryParseRange(ranges[0], "-", out oldStart, out oldLength))
            return false;

        if (!TryParseRange(ranges[1], "+", out newStart, out newLength))
            return false;

        return true;
    }

    private static bool TryParseRange(string s, string marker, out int start, out int length)
    {
        start = 0;
        length = 0;

        if (!s.StartsWith(marker))
            return false;

        var numbers = s.Substring(1).Split(',');
        if (numbers.Length != 1 && numbers.Length != 2)
            return false;

        if (!Int32.TryParse(numbers[0], out start))
            return false;

        if (numbers.Length == 1)
            length = 1;
        else if (!Int32.TryParse(numbers[1], out length))
            return false;

        return true;
    }

    private static IReadOnlyList<PatchLine> ParseLines(string content)
    {
        var lines = GetLines(content);
        var headerEnd = GetHeaderEnd(lines);

        var result = new List<PatchLine>(lines.Count);

        for (var i = 0; i <= headerEnd; i++)
        {
            var kind = PatchLineKind.Header;
            var text = lines[i].Line;
            var lineBreak = lines[i].LineBreak;
            if (text.StartsWith("diff --git"))
                kind = PatchLineKind.DiffLine;
            var line = new PatchLine(kind, text, lineBreak);
            result.Add(line);
        }

        for (var i = headerEnd + 1; i < lines.Count; i++)
        {
            var text = lines[i].Line;
            var lineBreak = lines[i].LineBreak;

            PatchLineKind kind;

            if (text.StartsWith("@@"))
                kind = PatchLineKind.Hunk;
            else if (text.StartsWith("+"))
                kind = PatchLineKind.Addition;
            else if (text.StartsWith("-"))
                kind = PatchLineKind.Removal;
            else if (text.StartsWith(@"\"))
                kind = PatchLineKind.NoEndOfLine;
            else
                kind = PatchLineKind.Context;

            var line = new PatchLine(kind, text, lineBreak);
            result.Add(line);
        }

        return result;
    }

    private static int GetHeaderEnd(IReadOnlyList<(string Line, string LineBreak)> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Line.StartsWith("+++"))
                return i;
        }

        return -1;
    }

    private static IReadOnlyList<(string Line, string LineBreak)> GetLines(string content)
    {
        var lines = new List<(string Line, string LineBreak)>();

        var beginningOfLine = 0;
        var i = 0;

        while (i < content.Length)
        {
            const char cr = '\r';
            const char lf = '\n';

            var c = content[i];
            var l = i < content.Length - 1 ? content[i + 1] : '\0';
            var lineBreakWidth = 0;

            if (c == cr && l == lf)
            {
                lineBreakWidth = 2;
            }
            else if (c is cr or lf)
            {
                lineBreakWidth = 1;
            }

            if (lineBreakWidth > 0)
            {
                CommitLine(lines, content, ref beginningOfLine, i, lineBreakWidth);
                i += lineBreakWidth;
            }
            else
            {
                i++;
            }
        }

        CommitLine(lines, content, ref beginningOfLine, i, 0);

        return lines.ToArray();

        static void CommitLine(List<(string Line, string LineBreak)> lines, string content, ref int beginningOfLine, int i, int lineBreakWidth)
        {
            if (beginningOfLine >= content.Length)
                return;

            var length = i - beginningOfLine;
            var line = content.Substring(beginningOfLine, length);
            var lineBreak = content.Substring(i, lineBreakWidth);
            lines.Add((line, lineBreak));
            beginningOfLine = i + lineBreakWidth;
        }
    }
}
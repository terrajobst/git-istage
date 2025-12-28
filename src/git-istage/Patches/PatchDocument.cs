using System.Diagnostics;
using GitIStage.UI;

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

    public static PatchDocument Create(string? pt, bool isStaged)
    {
        var p = GitIStage.Patching.Patch.Parse(pt ?? string.Empty);
        if (p == GitIStage.Patching.Patch.Empty)
            return new PatchDocument(Array.Empty<PatchEntry>(), Array.Empty<PatchLine>(), isStaged);

        var lines = new List<PatchLine>();
        var entries = new List<PatchEntry>();

        foreach (var pEntry in p.Entries)
        {
            var entryLines = pEntry.GetLines().Select(ToLine).ToArray();
            var entryOffset = lines.Count;
            var entryLength = entryLines.Length;

            lines.AddRange(entryLines);

            var hunks = new List<PatchHunk>();

            foreach (var pHunk in pEntry.Hunks)
            {
                var offset = pHunk.Header.TextLine.LineIndex;
                var length = pHunk.Lines.Last().TextLine.LineIndex - offset + 1;
                var oldStart = pHunk.Header.OldLine;
                var oldLength = pHunk.Header.OldCount;
                var newStart = pHunk.Header.NewLine;
                var newLength = pHunk.Header.NewCount;
                var hunk = new PatchHunk(offset, length, oldStart, oldLength, newStart, newLength);
                hunks.Add(hunk);
            }

            var entry = new PatchEntry(entryOffset, entryLength, pEntry.OldPath, pEntry.OldMode, pEntry.NewPath, pEntry.NewMode, hunks);
            entries.Add(entry);
        }

        return new PatchDocument(entries, lines, isStaged);

        PatchLine ToLine(GitIStage.Patching.PatchLine pLine)
        {
            var kind = ToLineKind(pLine.Kind);
            var sourceText = pLine.TextLine.Text;
            var textSpan = pLine.TextLine.Span;
            var lineBreakSpan = GitIStage.Patching.Text.TextSpan.FromBounds(textSpan.End, pLine.TextLine.SpanIncludingLineBreak.End); 
            var text = sourceText.ToString(textSpan);
            var lineBreak = sourceText.ToString(lineBreakSpan);
            return new PatchLine(kind, text, lineBreak);
        }

        PatchLineKind ToLineKind(GitIStage.Patching.PatchNodeKind pKind)
        {
            switch (pKind)
            {
                case GitIStage.Patching.PatchNodeKind.DiffGitHeader:
                    return PatchLineKind.DiffLine;
                case GitIStage.Patching.PatchNodeKind.OldPathHeader:
                case GitIStage.Patching.PatchNodeKind.NewPathHeader:
                case GitIStage.Patching.PatchNodeKind.OldModeHeader:
                case GitIStage.Patching.PatchNodeKind.NewModeHeader:
                case GitIStage.Patching.PatchNodeKind.DeletedFileModeHeader:
                case GitIStage.Patching.PatchNodeKind.NewFileModeHeader:
                case GitIStage.Patching.PatchNodeKind.CopyFromHeader:
                case GitIStage.Patching.PatchNodeKind.CopyToHeader:
                case GitIStage.Patching.PatchNodeKind.RenameFromHeader:
                case GitIStage.Patching.PatchNodeKind.RenameToHeader:
                case GitIStage.Patching.PatchNodeKind.SimilarityIndexHeader:
                case GitIStage.Patching.PatchNodeKind.DissimilarityIndexHeader:
                case GitIStage.Patching.PatchNodeKind.IndexHeader:
                case GitIStage.Patching.PatchNodeKind.UnknownHeader:
                    return PatchLineKind.Header;
                case GitIStage.Patching.PatchNodeKind.HunkHeader:
                    return PatchLineKind.Hunk;
                case GitIStage.Patching.PatchNodeKind.ContextLine:
                    return PatchLineKind.Context;
                case GitIStage.Patching.PatchNodeKind.AddedLine:
                    return PatchLineKind.Addition;
                case GitIStage.Patching.PatchNodeKind.DeletedLine:
                    return PatchLineKind.Removal;
                case GitIStage.Patching.PatchNodeKind.NoNewLine:
                    return PatchLineKind.NoEndOfLine;
                default:
                    throw new UnreachableException($"Unexpected kind {pKind}");
            }
        }
    }
}
using System.Diagnostics;
using System.Text;
using GitIStage.Text;

namespace GitIStage.Patches;

internal static class PatchExtensions
{
    public static bool IsAddedOrDeletedLine(this PatchNodeKind kind)
    {
        return kind is PatchNodeKind.AddedLine or
                       PatchNodeKind.DeletedLine;
    }

    public static string? GetTokenText(this PatchNodeKind kind)
    {
        return kind switch
        {
            PatchNodeKind.DiffKeyword => "diff",
            PatchNodeKind.GitKeyword => "git",
            PatchNodeKind.IndexKeyword => "index",
            PatchNodeKind.NewKeyword => "new",
            PatchNodeKind.FileKeyword => "file",
            PatchNodeKind.ModeKeyword => "mode",
            PatchNodeKind.DeletedKeyword => "deleted",
            PatchNodeKind.OldKeyword => "old",
            PatchNodeKind.CopyKeyword => "copy",
            PatchNodeKind.FromKeyword => "from",
            PatchNodeKind.ToKeyword => "to",
            PatchNodeKind.RenameKeyword => "rename",
            PatchNodeKind.SimilarityKeyword => "similarity",
            PatchNodeKind.DissimilarityKeyword => "dissimilarity",
            PatchNodeKind.BinaryKeyword => "Binary",
            PatchNodeKind.FilesKeyword => "files",
            PatchNodeKind.AndKeyword => "and",
            PatchNodeKind.DifferKeyword => "differ",
            PatchNodeKind.PercentageToken => "%",
            PatchNodeKind.MinusMinusToken => "--",
            PatchNodeKind.MinusMinusMinusToken => "---",
            PatchNodeKind.PlusPlusPlusToken => "+++",
            PatchNodeKind.DotDotToken => "..",
            PatchNodeKind.AtAtToken => "@@",
            PatchNodeKind.MinusToken => "-",
            PatchNodeKind.PlusToken => "+",
            PatchNodeKind.SpaceToken => " ",
            PatchNodeKind.BackslashToken => "\\",
            _ => null
        };
    }

    public static bool IsKeyword(this PatchNodeKind kind)
    {
        return kind switch
        {
            PatchNodeKind.DiffKeyword or
            PatchNodeKind.GitKeyword or
            PatchNodeKind.IndexKeyword or
            PatchNodeKind.NewKeyword or
            PatchNodeKind.FileKeyword or
            PatchNodeKind.ModeKeyword or
            PatchNodeKind.DeletedKeyword or
            PatchNodeKind.OldKeyword or
            PatchNodeKind.CopyKeyword or
            PatchNodeKind.FromKeyword or
            PatchNodeKind.ToKeyword or
            PatchNodeKind.RenameKeyword or
            PatchNodeKind.SimilarityKeyword or
            PatchNodeKind.DissimilarityKeyword or
            PatchNodeKind.BinaryKeyword or
            PatchNodeKind.FilesKeyword or
            PatchNodeKind.AndKeyword or
            PatchNodeKind.DifferKeyword  => true,
            _ => false
        };
    }

    public static bool IsOperator(this PatchNodeKind kind)
    {
        return kind switch
        {
            PatchNodeKind.MinusMinusToken or
            PatchNodeKind.MinusMinusMinusToken or
            PatchNodeKind.PlusPlusPlusToken or
            PatchNodeKind.DotDotToken or
            PatchNodeKind.AtAtToken or
            PatchNodeKind.MinusToken or
            PatchNodeKind.PlusToken or
            PatchNodeKind.SpaceToken or
            PatchNodeKind.BackslashToken => true,
            _ => false
        };
    }

    public static string ToOctalString(this PatchEntryMode mode)
    {
        return Convert.ToString((int)mode, 8);
    }

    public static Patch SelectForApplication(this Patch patch, IEnumerable<int> lineIndexes, PatchDirection direction)
    {
        var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;

        var newPatch = new StringBuilder();

        var entryLines = lineIndexes.Select(i => (LineIndex: i, Entry: FindEntry(patch, i)!))
                                    .GroupBy(t => t.Entry, t => t.LineIndex);

        foreach (var entryLine in entryLines)
        {
            var entry = entryLine.Key;
            var hunkLines = entryLine.Select(i => (LineIndex: i, Hunk: FindHunk(entry, i)!))
                                     .GroupBy(t => t.Hunk, t => t.LineIndex);

            foreach (var hunkLineGroup in hunkLines)
            {
                var hunk = hunkLineGroup.Key;
                var lineSet = new HashSet<int>(hunkLineGroup);

                // If we're staging a line after '\ No newline at end of file', that means we're modifying the final
                // line in a file, and that line had no line break. If we want to add a new line after it, by definition
                // we must include the current last line as a removal because we need to add a line break.
                //
                // For example, consider this case:
                //
                //     @@ -1,2 +1,2 @@
                //      Line 1
                //     -Line 2
                //     \ No newline at end of file
                //     +Line 2 Changed
                //     +Line 3
                //     +Line 4
                //     \ No newline at end of file
                //
                // If we only stage '+Line 3' we need to generate this diff:
                //
                //     @@ -1,2 +1,2 @@
                //      Line 1
                //     -Line 2
                //     \ No newline at end of file
                //     +Line 2
                //     +Line 3
                //     \ No newline at end of file
                //
                // For more details, see docs/handling-no-newline.md

                var lastLineInHunk = hunkLineGroup.Max();
                var noFinalLineBreakLine = hunk.Lines
                    .FirstOrDefault(l => l.LineIndex < lastLineInHunk && l.Kind == PatchNodeKind.NoFinalLineBreakLine);
                var finalLine = noFinalLineBreakLine is null || noFinalLineBreakLine.LineIndex == 0
                                    ? null
                                    : patch.Lines[noFinalLineBreakLine.LineIndex - 1] as PatchHunkLine; 

                // Get current hunk information

                var oldStart = hunk.OldRange.LineNumber;
                var newStart = hunk.NewRange.LineNumber;

                // Compute the new hunk size

                var oldLength = 0;

                foreach (var line in hunk.Lines)
                {
                    var kind = line.Kind;
                    var i = line.TextLine.LineIndex;

                    var wasPresent = kind == PatchNodeKind.ContextLine ||
                                     kind == PatchNodeKind.DeletedLine && (!isUndo || lineSet.Contains(i)) ||
                                     kind == PatchNodeKind.AddedLine && (isUndo && !lineSet.Contains(i));

                    if (wasPresent)
                        oldLength++;
                }

                var delta = lineSet.Select(i => patch.Lines[i].Kind)
                                   .Where(k => k.IsAddedOrDeletedLine())
                                   .Select(k => k == PatchNodeKind.AddedLine ? 1 : -1)
                                   .Sum();

                var newLength = oldLength + delta;

                // Check if we're staging a deletion

                var allLinesSelected = hunk.Lines.Select(l => l.TextLine.LineIndex)
                                                 .All(i => lineSet.Contains(i));

                // Add header
                newPatch.Append(entry.Header.Text);
                newPatch.Append(entry.Header.LineBreak);

                var changes = entry;
                var isAddition = changes.OldMode == PatchEntryMode.Nonexistent;
                var isDeletion = changes.NewMode == PatchEntryMode.Nonexistent;

                if (isAddition && direction == PatchDirection.Reset)
                {
                    newPatch.Append($"--- a/{changes.NewPath}\n");
                    newPatch.Append($"+++ b/{changes.NewPath}\n");
                }
                else
                {
                    if (!isAddition)
                    {
                        newPatch.Append($"--- a/{changes.OldPath}\n");
                    }
                    else
                    {
                        newPatch.Append($"new file mode {changes.NewMode.ToOctalString()}\n");
                        newPatch.Append("--- /dev/null\n");
                    }

                    if (!isDeletion || !allLinesSelected)
                    {
                        newPatch.Append($"+++ b/{changes.NewPath}\n");
                    }
                    else
                    {
                        newPatch.Append($"deleted file mode {changes.OldMode.ToOctalString()}\n");
                        newPatch.Append("+++ /dev/null\n");
                    }
                }

                // Write hunk header

                newPatch.Append("@@ -");
                newPatch.Append(oldStart);
                if (oldLength != 1)
                {
                    newPatch.Append(',');
                    newPatch.Append(oldLength);
                }
                newPatch.Append(" +");
                newPatch.Append(newStart);
                if (newLength != 1)
                {
                    newPatch.Append(',');
                    newPatch.Append(newLength);
                }
                newPatch.Append(" @@");
                newPatch.Append('\n');

                // Write hunk

                var previousIncluded = false;

                foreach (var line in hunk.Lines)
                {
                    var kind = line.Kind;
                    var i = line.TextLine.LineIndex;

                    if (lineSet.Contains(i) ||
                        kind == PatchNodeKind.ContextLine ||
                        previousIncluded && kind == PatchNodeKind.NoFinalLineBreakLine ||
                        line == finalLine || line == noFinalLineBreakLine)
                    {
                        newPatch.Append(line.Text);
                        newPatch.Append(line.LineBreak);
                        previousIncluded = true;

                        if (line == noFinalLineBreakLine)
                        {
                            Debug.Assert(finalLine is not null);
                            newPatch.Append('+');
                            newPatch.Append(finalLine.Text.Slice(1));
                            newPatch.Append(finalLine.LineBreak);
                        }
                    }
                    else if (!isUndo && kind == PatchNodeKind.DeletedLine ||
                             isUndo && kind == PatchNodeKind.AddedLine)
                    {
                        newPatch.Append(' ');
                        newPatch.Append(line.Text.Slice(1));
                        newPatch.Append(line.LineBreak);
                        previousIncluded = true;
                    }
                    else
                    {
                        previousIncluded = false;
                    }
                }
            }
        }

        return Patch.Parse(newPatch.ToString());

        static PatchEntry? FindEntry(Patch patch, int lineIndex)
        {
            var line = patch.Lines[lineIndex];
            return line.AncestorsAndSelf().OfType<PatchEntry>().FirstOrDefault();
        }

        static PatchHunk? FindHunk(PatchEntry entry, int lineIndex)
        {
            var line = entry.Root.Lines[lineIndex];
            return line.AncestorsAndSelf().OfType<PatchHunk>().FirstOrDefault();
        }
    }

    public static (int Added, int Modified, int Deleted) GetFileStatistics(this Patch patch)
    {
        var added = 0;
        var modified = 0;
        var deleted = 0;

        foreach (var entry in patch.Entries)
        {
            if (entry.Change == PatchEntryChange.Added)
                added++;
            else if (entry.Change == PatchEntryChange.Deleted)
                deleted++;
            else
                modified++;
        }

        return (added, modified, deleted);
    }

    // TODO: I think we'll want some unit tests for this method
    public static Patch Update(this Patch patch, HashSet<string> affectedPaths, Patch patchForAffectedPaths)
    {
        var partialPatchEntryByPath = patchForAffectedPaths.Entries.ToDictionary(GetPath);
        var entries = new List<PatchEntry>();

        foreach (var oldEntry in patch.Entries)
        {
            var path = GetPath(oldEntry);
            if (partialPatchEntryByPath.TryGetValue(path, out var newEntry))
                entries.Add(newEntry);
            else if (!affectedPaths.Contains(path))
                entries.Add(oldEntry);
        }

        var sb = new StringBuilder();
        foreach (var entry in entries)
            WriteEntry(sb, entry);

        var result = Patch.Parse(sb.ToString());
        return result;

        static string GetPath(PatchEntry e)
        {
            return string.IsNullOrEmpty(e.NewPath) ? e.OldPath : e.NewPath;
        }

        static void WriteEntry(StringBuilder sb, PatchEntry e)
        {
            var text = e.Root.Text;
            var firstLine = text.GetLineIndex(e.Span.Start);
            var lastLine = text.GetLineIndex(e.Span.End);
            var entryStart = text.Lines[firstLine].Start;
            var entryEnd = text.Lines[lastLine].SpanIncludingLineBreak.End;
            var span = TextSpan.FromBounds(entryStart, entryEnd);
            sb.Append(text.AsSpan(span));
        }
    }
}
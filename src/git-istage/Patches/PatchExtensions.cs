using System.Text;

namespace GitIStage.Patches;

internal static class PatchExtensions
{
    public static bool IsAddedOrDeletedLine(this PatchNodeKind kind)
    {
        return kind is PatchNodeKind.AddedLine or
                       PatchNodeKind.DeletedLine;
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

            foreach (var hunkLine in hunkLines)
            {
                var hunk = hunkLine.Key;
                var lineSet = new HashSet<int>(hunkLine);

                // Get current hunk information

                var oldStart = hunk.Header.OldLine;
                var newStart = hunk.Header.NewLine;

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

                var delta = lineSet.Select(i => patch.Lines[i])
                                   .Select(l => l.Kind)
                                   .Where(k => k.IsAddedOrDeletedLine())
                                   .Select(k => k == PatchNodeKind.AddedLine ? 1 : -1)
                                   .Sum();

                var newLength = oldLength + delta;

                // Add header
                newPatch.Append(entry.Headers[0].Text);
                newPatch.Append(entry.Headers[0].LineBreak);

                var changes = entry;
                var oldPath = changes.OldPath;
                var oldExists = oldLength != 0 || changes.OldMode != 0;
                var newPath = changes.NewPath;

                if (oldExists)
                {
                    newPatch.Append($"--- a/{oldPath}\n");
                }
                else
                {
                    newPatch.Append($"new file mode {Convert.ToString(changes.NewMode, 8)}\n");
                    newPatch.Append("--- /dev/null\n");
                }

                newPatch.Append($"+++ b/{newPath}\n");

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
                        previousIncluded && kind == PatchNodeKind.NoNewLine)
                    {
                        newPatch.Append(line.Text);
                        newPatch.Append(line.LineBreak);
                        previousIncluded = true;
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

    public static (int Added, int Modified, int Deleted) GetStats(this Patch patch)
    {
        var added = 0;
        var modified = 0;
        var deleted = 0;

        foreach (var entry in patch.Entries)
        {
            if (entry.ChangeKind == PatchEntryChangeKind.Added)
                added++;
            else if (entry.ChangeKind == PatchEntryChangeKind.Deleted)
                deleted++;
            else
                modified++;
        }

        return (added, modified, deleted);
    }
}
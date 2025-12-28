using System.Text;
using LibGit2Sharp;

namespace GitIStage.Patches;

internal static class Patching
{
    // TODO: Use document.Patch instead
    public static string ComputePatch(PatchDocument document, IEnumerable<int> lineIndexes, PatchDirection direction)
    {
        var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;

        var newPatch = new StringBuilder();

        var entryLines = lineIndexes.Select(i => (LineIndex: i, Entry: document.FindEntry(i)!))
                                    .GroupBy(t => t.Entry, t => t.LineIndex);

        foreach (var entryLine in entryLines)
        {
            var entry = entryLine.Key;
            var hunkLines = entryLine.Select(i => (LineIndex: i, Hunk: entry.FindHunk(i)!))
                                     .GroupBy(t => t.Hunk, t => t.LineIndex);

            foreach (var hunkLine in hunkLines)
            {
                var hunk = hunkLine.Key;
                var lineSet = new HashSet<int>(hunkLine);

                // Get current hunk information

                var oldStart = hunk.OldStart;
                var newStart = hunk.NewStart;

                // Compute the new hunk size

                var oldLength = 0;

                for (var i = hunk.Offset; i < hunk.Offset + hunk.Length; i++)
                {
                    var line = document.Lines[i];
                    var kind = line.Kind;

                    var wasPresent = kind == PatchLineKind.Context ||
                                     kind == PatchLineKind.Removal && (!isUndo || lineSet.Contains(i)) ||
                                     kind == PatchLineKind.Addition && (isUndo && !lineSet.Contains(i));

                    if (wasPresent)
                        oldLength++;
                }

                var delta = lineSet.Select(i => document.Lines[i])
                                   .Select(l => l.Kind)
                                   .Where(k => k.IsAdditionOrRemoval())
                                   .Select(k => k == PatchLineKind.Addition ? 1 : -1)
                                   .Sum();

                var newLength = oldLength + delta;

                // Add header

                var changes = entry;
                var oldPath = changes.OldPath.Replace(@"\", "/");
                var oldExists = oldLength != 0 || changes.OldMode != 0;
                var path = changes.NewPath.Replace(@"\", "/");

                if (oldExists)
                {
                    newPatch.Append($"--- {oldPath}\n");
                }
                else
                {
                    newPatch.Append($"new file mode {Convert.ToString(changes.NewMode, 8)}\n");
                    newPatch.Append("--- /dev/null\n");
                }

                newPatch.Append($"+++ b/{path}\n");

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

                for (var i = hunk.Offset; i < hunk.Offset + hunk.Length; i++)
                {
                    var line = document.Lines[i];
                    var kind = line.Kind;
                    if (lineSet.Contains(i) ||
                        kind == PatchLineKind.Context ||
                        previousIncluded && kind == PatchLineKind.NoEndOfLine)
                    {
                        newPatch.Append(line.Text);
                        newPatch.Append(line.LineBreak);
                        previousIncluded = true;
                    }
                    else if (!isUndo && kind == PatchLineKind.Removal ||
                             isUndo && kind == PatchLineKind.Addition)
                    {
                        newPatch.Append(' ');
                        newPatch.Append(line.Text, 1, line.Text.Length - 1);
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

        return newPatch.ToString();
    }
}
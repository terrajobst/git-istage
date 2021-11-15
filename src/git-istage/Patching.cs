using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using LibGit2Sharp;

namespace GitIStage
{
    internal static class Patching
    {
        public static string ComputePatch(PatchDocument document, IEnumerable<int> lineIndexes, PatchDirection direction)
        {
            var isUndo = direction == PatchDirection.Reset ||
                         direction == PatchDirection.Unstage;

            var newPatch = new StringBuilder();

            var entryLines = lineIndexes.Select(i => new {i, e = document.FindEntry(i)}).GroupBy(t => t.e, t => t.i);

            foreach (var entryLine in entryLines)
            {
                var entry = entryLine.Key;
                var hunkLines = entryLine.Select(i => new {i, h = entry.FindHunk(i)}).GroupBy(t => t.h, t => t.i);

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

                    var changes = entry.Changes;
                    var oldPath = changes.OldPath.Replace(@"\", "/");
                    var oldExists = oldLength != 0 || changes.OldMode != Mode.Nonexistent;
                    var path = changes.Path.Replace(@"\", "/");

                    if (oldExists)
                    {
                        newPatch.AppendFormat("--- {0}\n", oldPath);
                    }
                    else
                    {
                        newPatch.AppendFormat("new file mode {0}\n", changes.Mode);
                        newPatch.Append("--- /dev/null\n");
                    }
                    newPatch.AppendFormat("+++ b/{0}\n", path);

                    // Write hunk header

                    newPatch.Append("@@ -");
                    newPatch.Append(oldStart);
                    if (oldLength != 1)
                    {
                        newPatch.Append(",");
                        newPatch.Append(oldLength);
                    }
                    newPatch.Append(" +");
                    newPatch.Append(newStart);
                    if (newLength != 1)
                    {
                        newPatch.Append(",");
                        newPatch.Append(newLength);
                    }
                    newPatch.Append(" @@");
                    newPatch.Append("\n");

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
                            newPatch.Append("\n");
                            previousIncluded = true;
                        }
                        else if (!isUndo && kind == PatchLineKind.Removal ||
                                 isUndo && kind == PatchLineKind.Addition)
                        {
                            newPatch.Append(" ");
                            newPatch.Append(line.Text, 1, line.Text.Length - 1);
                            newPatch.Append("\n");
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

        public static void ApplyPatch(string pathToGit, string workingDirectory, string patch, PatchDirection direction)
        {
            var isUndo = direction == PatchDirection.Reset ||
                         direction == PatchDirection.Unstage;
            var patchFilePath = Path.GetTempFileName();
            var reverse = isUndo ? "--reverse" : string.Empty;
            var cached = direction == PatchDirection.Reset ? string.Empty : "--cached";
            var arguments = $@"apply {cached} {reverse} --whitespace=nowarn ""{patchFilePath}""";

            File.WriteAllText(patchFilePath, patch);
            var startInfo = new ProcessStartInfo
            {
                FileName = pathToGit,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var process = new Process())
            {
                var output = new List<string>();

                DataReceivedEventHandler handler = (s, e) =>
                {
                    lock (output)
                    {
                        if (e.Data != null)
                            output.Add(e.Data);
                    }
                };

                process.StartInfo = startInfo;
                process.OutputDataReceived += handler;
                process.ErrorDataReceived += handler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (output.Any(l => l.Trim().Length > 0))
                {
                    Console.Clear();
                    foreach (var line in output)
                        Console.WriteLine(line.Trim());

                    // foreach (var line in patch.Split('\n'))
                    //     Console.WriteLine(line);
                    Console.Write(patch);
                     
                    Console.ReadKey();
                }
            }

            File.Delete(patchFilePath);
        }
    }
}

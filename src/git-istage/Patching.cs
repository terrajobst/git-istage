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
        private static bool TryGetHunkInformation(string hunkLine,
                                                  out int oldStart,
                                                  out int oldLength,
                                                  out int newStart,
                                                  out int newLength,
                                                  out int headerLength)
        {
            oldStart = 0;
            oldLength = 0;
            newStart = 0;
            newLength = 0;
            headerLength = 0;

            if (!hunkLine.StartsWith("@@"))
                return false;

            var hunkMarkerEnd = hunkLine.IndexOf("@@", 2, StringComparison.Ordinal);
            if (hunkMarkerEnd < 0)
                return false;

            headerLength = hunkMarkerEnd + 2;

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

            if (!int.TryParse(numbers[0], out start))
                return false;

            if (numbers.Length == 1)
                length = 1;
            else if (!int.TryParse(numbers[1], out length))
                return false;

            return true;
        }

        public static PatchLineKind GetPatchLineKind(IReadOnlyList<string> patchLines, int lineIndex)
        {
            var headerEnd = GetHeaderEnd(patchLines);
            var line = patchLines[lineIndex];

            if (lineIndex <= headerEnd)
                return PatchLineKind.Header;

            if (line.StartsWith("@@"))
                return PatchLineKind.Hunk;

            if (line.StartsWith("+"))
                return PatchLineKind.Addition;

            if (line.StartsWith("-"))
                return PatchLineKind.Removal;

            if (line.StartsWith(@"\"))
                return PatchLineKind.NoEndOfLine;

            return PatchLineKind.Context;
        }

        private static int GetHeaderEnd(IReadOnlyList<string> patchLines)
        {
            for (var i = 0; i < patchLines.Count; i++)
            {
                if (patchLines[i].StartsWith("+++"))
                    return i;
            }

            return -1;
        }

        public static string Stage(PatchFile patchFile, int stageIndex, bool isUnstaging)
        {
            var patchLines = patchFile.Lines;

            // Find enclosing hunk

            var hunkStart = stageIndex;
            while (hunkStart - 1 >= 0 && patchLines[hunkStart - 1].Kind != PatchLineKind.Hunk)
                hunkStart--;

            var hunkEnd = stageIndex;
            while (hunkEnd + 1 < patchLines.Count && patchLines[hunkEnd + 1].Kind != PatchLineKind.Hunk)
                hunkEnd++;

            // Get current hunk information

            var hunkHeaderLine = patchLines[hunkStart - 1];

            int oldStart;
            int oldLength;
            int newStart;
            int newLength;
            int headerLength;
            TryGetHunkInformation(hunkHeaderLine.Text,
                                  out oldStart,
                                  out oldLength,
                                  out newStart,
                                  out newLength,
                                  out headerLength);

            // Compue the new hunk size

            oldLength = 0;

            for (var i = hunkStart; i <= hunkEnd; i++)
            {
                var line = patchLines[i];
                var kind = line.Kind;
                var isIncluded = kind == PatchLineKind.Context ||
                                 kind == PatchLineKind.Removal && !isUnstaging && i == stageIndex ||
                                 kind == PatchLineKind.Addition && isUnstaging && i != stageIndex;
                if (isIncluded)
                    oldLength++;
            }

            if (patchLines[stageIndex].Kind == PatchLineKind.Addition)
                newLength = oldLength + 1;
            else
                newLength = oldLength - 1;

            // Write the patch

            var newPatch = new StringBuilder();

            // Add header

            var changes = patchFile.Patch.Single();
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

            for (var i = hunkStart; i <= hunkEnd; i++)
            {
                var line = patchLines[i];
                var kind = patchLines[i].Kind;
                if (i == stageIndex ||
                    kind == PatchLineKind.Context ||
                    previousIncluded && kind == PatchLineKind.NoEndOfLine)
                {
                    newPatch.Append(line.Text);
                    newPatch.Append("\n");
                    previousIncluded = true;
                }
                else if (!isUnstaging && kind == PatchLineKind.Removal ||
                         isUnstaging && kind == PatchLineKind.Addition)
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

            return newPatch.ToString();
        }

        public static void ApplyPatch(string workingDirectory, string patch, bool isUnstaging)
        {
            var patchFilePath = Path.GetTempFileName();
            var reverse = isUnstaging ? "--reverse" : string.Empty;
            var arguments = $@"apply --cached {reverse} --whitespace=nowarn ""{patchFilePath}""";

            File.WriteAllText(patchFilePath, patch);
            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Git\bin\git.exe",
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
                process.StartInfo = startInfo;
                process.OutputDataReceived += (s, e) =>
                {
                    lock (output)
                        if (e.Data != null)
                            output.Add(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    lock (output)
                        if (e.Data != null)
                            output.Add(e.Data);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (output.Any(l => l.Trim().Length > 0))
                {
                    Console.Clear();
                    foreach (var line in output)
                        Console.WriteLine(line);

                    Console.Write(patch);
                    Console.ReadKey();
                }
            }

            File.Delete(patchFilePath);
        }
    }
}

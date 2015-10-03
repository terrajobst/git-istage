using System;
using System.Collections.Generic;
using System.IO;

using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class PatchFile
    {
        public PatchFile(Patch patch, string path)
        {
            Patch = patch;
            Path = path;
            Lines = GetPatchLines(patch.Content.Replace("\t", "    "));
        }

        public Patch Patch { get; }

        public string Path { get; set; }

        public IReadOnlyList<PatchLine> Lines { get; }

        private IReadOnlyList<PatchLine> GetPatchLines(string content)
        {
            var textLines = GetLines(content);
            var result = new PatchLine[textLines.Count];
            for (var i = 0; i < textLines.Count; i++)
                result[i] = new PatchLine(this, textLines, i);
            return result;
        } 

        private static IReadOnlyList<string> GetLines(string content)
        {
            var lines = new List<string>();

            using (var sr = new StringReader(content))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }
    }
}
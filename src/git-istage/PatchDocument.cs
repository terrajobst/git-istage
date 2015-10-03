using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GitIStage
{
    internal class PatchDocument : Document
    {
        public PatchDocument(IEnumerable<PatchFile> patchFiles)
        {
            Files = new ReadOnlyCollection<PatchFile>(patchFiles.ToArray());
            Lines = new ReadOnlyCollection<PatchLine>(Files.SelectMany(p => p.Lines).ToArray());
            Width = Lines.Select(l => l.Text).DefaultIfEmpty(string.Empty).Max(t => t.Length);
        }

        public ReadOnlyCollection<PatchFile> Files { get; }

        public ReadOnlyCollection<PatchLine> Lines { get; }

        public override int Height => Lines.Count;

        public override int Width { get; }
    }
}
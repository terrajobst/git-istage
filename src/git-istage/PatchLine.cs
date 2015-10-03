using System;
using System.Collections.Generic;

namespace GitIStage
{
    internal sealed class PatchLine
    {
        public PatchLine(PatchFile patchFile, IReadOnlyList<string> patchLines, int index)
        {
            PatchFile = patchFile;
            Kind = Patching.GetPatchLineKind(patchLines, index);
            Text = patchLines[index];
            Index = index;
        }

        public PatchFile PatchFile { get; }

        public PatchLineKind Kind { get; }

        public string Text { get; }

        public int Index { get; }
    }
}
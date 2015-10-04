using System;
using System.Collections.Generic;
using System.Linq;

using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class PatchEntry
    {
        public PatchEntry(int offset, int length, Patch patch, PatchEntryChanges changes, IReadOnlyList<PatchHunk> hunks)
        {
            Offset = offset;
            Length = length;
            Patch = patch;
            Changes = changes;
            Hunks = hunks;
        }

        public int Offset { get; set; }

        public int Length { get; set; }

        public Patch Patch { get; set; }

        public PatchEntryChanges Changes { get; set; }

        public IReadOnlyList<PatchHunk> Hunks { get; set; }

        public PatchHunk FindHunk(int lineIndex)
        {
            // TODO: Binary search would be more appropriate

            return Hunks.Single(h => h.Offset <= lineIndex && lineIndex < h.Offset + h.Length);
        }
    }
}
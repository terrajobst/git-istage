using LibGit2Sharp;

namespace GitIStage.Patches;

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
        var hunkIndex = FindHunkIndex(lineIndex);
        return hunkIndex < 0 ? null : Hunks[hunkIndex];
    }

    public int FindHunkIndex(int lineIndex)
    {
        // TODO: Binary search would be more appropriate

        for (var i = 0; i < Hunks.Count; i++)
        {
            var h = Hunks[i];
            if (h.Offset <= lineIndex && lineIndex < h.Offset + h.Length)
                return i;
        }

        return -1;
    }
}
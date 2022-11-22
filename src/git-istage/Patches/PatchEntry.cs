using LibGit2Sharp;

namespace GitIStage.Patches;

internal sealed class PatchEntry
{
    public PatchEntry(int offset, int length, PatchEntryChanges changes, IReadOnlyList<PatchHunk> hunks)
    {
        Offset = offset;
        Length = length;
        Changes = changes;
        Hunks = hunks;
    }

    public int Offset { get; }

    public int Length { get; }

    public PatchEntryChanges Changes { get; }

    public IReadOnlyList<PatchHunk> Hunks { get; }

    public PatchHunk? FindHunk(int lineIndex)
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
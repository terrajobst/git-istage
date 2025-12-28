namespace GitIStage.Patches;

// TODO: Delete this in favor of the new patches
internal sealed class PatchEntry
{
    public PatchEntry(int offset,
                      int length,
                      string oldPath,
                      int oldMode,
                      string newPath,
                      int newMode,
                      IReadOnlyList<PatchHunk> hunks)
    {
        Offset = offset;
        Length = length;
        OldPath = oldPath;
        OldMode = oldMode;
        NewPath = newPath;
        NewMode = newMode;
        Hunks = hunks;
    }

    public int Offset { get; }

    public int Length { get; }

    public IReadOnlyList<PatchHunk> Hunks { get; }

    public string NewPath { get; }

    public int NewMode { get; }

    public string OldPath { get; }

    public int OldMode { get; }

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
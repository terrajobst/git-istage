namespace GitIStage.Patches;

// TODO: Delete this in favor of the new patches
internal sealed class PatchHunk
{
    public PatchHunk(int offset, int length, int oldStart, int oldLength, int newStart, int newLength)
    {
        Offset = offset;
        Length = length;
        OldStart = oldStart;
        OldLength = oldLength;
        NewStart = newStart;
        NewLength = newLength;
    }

    public int Offset { get; }

    public int Length { get; }

    public int OldStart { get; }

    public int OldLength { get; }

    public int NewStart { get; }

    public int NewLength { get; }
}
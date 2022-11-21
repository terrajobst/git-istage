namespace GitIStage;

internal static class PatchExtension
{
    public static bool IsAdditionOrRemoval(this PatchLineKind kind)
    {
        return kind == PatchLineKind.Addition ||
               kind == PatchLineKind.Removal;
    }

    public static int FindPreviousEntryIndex(this PatchDocument document, int lineIndex)
    {
        var entryIndex = document.FindEntryIndex(lineIndex);
        return Math.Max(entryIndex - 1, 0);
    }

    public static int FindNextEntryIndex(this PatchDocument document, int lineIndex)
    {
        var entryIndex = document.FindEntryIndex(lineIndex);
        return Math.Min(entryIndex + 1, document.Entries.Count - 1);
    }

    public static int FindPreviousChangeBlock(this PatchDocument document, int lineIndex)
    {
        var start = lineIndex;

        // Skip current block
        while (start > 0 && document.Lines[start].Kind.IsAdditionOrRemoval())
            start--;

        // Find next block
        while (start > 0 && !document.Lines[start].Kind.IsAdditionOrRemoval())
            start--;

        if (start < 0 || !document.Lines[start].Kind.IsAdditionOrRemoval())
            return lineIndex;

        return start;
    }

    public static int FindNextChangeBlock(this PatchDocument document, int lineIndex)
    {
        var end = lineIndex;

        // Skip current block
        while (end < document.Lines.Count - 1 && document.Lines[end].Kind.IsAdditionOrRemoval())
            end++;

        // Find next block
        while (end < document.Lines.Count - 1 && !document.Lines[end].Kind.IsAdditionOrRemoval())
            end++;

        if (end >= document.Lines.Count || !document.Lines[end].Kind.IsAdditionOrRemoval())
            return lineIndex;

        return end;
    }

    public static int FindStartOfChangeBlock(this PatchDocument document, int lineIndex)
    {
        var start = lineIndex;
        while (start > 0 && document.Lines[start - 1].Kind.IsAdditionOrRemoval())
            start--;

        return start;
    }

    public static int FindEndOfChangeBlock(this PatchDocument document, int lineIndex)
    {
        var end = lineIndex;
        while (end < document.Lines.Count - 1 && document.Lines[end + 1].Kind.IsAdditionOrRemoval())
            end++;

        return end;
    }
}
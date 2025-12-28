namespace GitIStage.Patches;


internal static class PatchExtension
{
    public static bool IsAdditionOrRemoval(this PatchLineKind kind)
    {
        return kind is PatchLineKind.Addition or
                       PatchLineKind.Removal;
    }

    // TODO: Use document.Patch instead
    public static int FindPreviousChangeBlock(this PatchDocument document, int lineIndex)
    {
        var start = lineIndex;

        // Skip current block
        while (start >= 0 && document.Lines[start].Kind.IsAdditionOrRemoval())
            start--;

        // Find next block
        while (start >= 0 && !document.Lines[start].Kind.IsAdditionOrRemoval())
            start--;

        if (start < 0)
            return FindStartOfChangeBlock(document, lineIndex);

        return start;
    }

    // TODO: Use document.Patch instead
    public static int FindNextChangeBlock(this PatchDocument document, int lineIndex)
    {
        var end = lineIndex;

        // Skip current block
        while (end < document.Lines.Count && document.Lines[end].Kind.IsAdditionOrRemoval())
            end++;

        // Find next block
        while (end < document.Lines.Count && !document.Lines[end].Kind.IsAdditionOrRemoval())
            end++;

        if (end >= document.Lines.Count)
            return FindEndOfChangeBlock(document, lineIndex);

        return end;
    }

    // TODO: Use document.Patch instead
    public static int FindStartOfChangeBlock(this PatchDocument document, int lineIndex)
    {
        var start = lineIndex;
        while (start > 0 && document.Lines[start - 1].Kind.IsAdditionOrRemoval())
            start--;

        return start;
    }

    // TODO: Use document.Patch instead
    public static int FindEndOfChangeBlock(this PatchDocument document, int lineIndex)
    {
        var end = lineIndex;
        while (end < document.Lines.Count - 1 && document.Lines[end + 1].Kind.IsAdditionOrRemoval())
            end++;

        return end;
    }
}
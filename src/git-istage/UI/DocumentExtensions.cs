namespace GitIStage.UI;

internal static class DocumentExtensions
{
    public static int FindPreviousEntryIndex(this Document document, int lineIndex)
    {
        var entryIndex = document.FindEntryIndex(lineIndex);
        return Math.Max(entryIndex - 1, 0);
    }

    public static int FindNextEntryIndex(this Document document, int lineIndex)
    {
        var entryIndex = document.FindEntryIndex(lineIndex);
        return Math.Min(entryIndex + 1, document.EntryCount - 1);
    }
}
using System.Diagnostics;
using GitIStage.Patches;

namespace GitIStage.UI;

// TODO: OK, this simplified the Document base type, but it feels like we want some help from the document
//       here. Maybe the way to do this is by having an interface, like IPatchNavigable or something plus
//       some extension methods on Patch.
internal static class DocumentExtensions
{
    // Navigate between files

    public static int FindPreviousEntryIndex(this Document document, int lineIndex)
    {
        var entryIndex = document.FindEntryIndex(lineIndex);
        return int.Max(entryIndex - 1, -1);
    }

    public static int FindNextEntryIndex(this Document document, int lineIndex)
    {
        if (document is PatchDocument patchDocument)
        {
            var entryIndex = document.FindEntryIndex(lineIndex);
            return entryIndex == patchDocument.Patch.Entries.Length - 1 ? -1 : entryIndex + 1;
        }

        if (document is FileDocument fileDocument)
        {
            var entryIndex = document.FindEntryIndex(lineIndex);
            return entryIndex == fileDocument.Patch.Entries.Length - 1 ? -1 : entryIndex + 1;
        }

        throw new UnreachableException($"Unexpected document type: {document.GetType()}");
    }

    public static int FindEntryIndex(this Document document, int lineIndex)
    {
        if (document is PatchDocument patchDocument)
        {
            var line = patchDocument.Patch.Lines[lineIndex];
            var entry = line.AncestorsAndSelf().OfType<PatchEntry>().FirstOrDefault();
            return entry is null ? -1 : patchDocument.Patch.Entries.IndexOf(entry);
        }

        if (document is FileDocument fileDocument)
        {
            var entry = fileDocument.GetEntry(lineIndex);
            return entry is null ? -1 : fileDocument.Patch.Entries.IndexOf(entry);
        }

        return -1;
    }

    public static int GetLineIndex(this Document document, int entryIndex)
    {
        if (document is PatchDocument patchDocument)
        {
            var entry = patchDocument.Patch.Entries[entryIndex];
            return entry.Headers.First().TextLine.LineIndex;
        }

        if (document is FileDocument fileDocument)
        {
            var entry = fileDocument.Patch.Entries[entryIndex];
            return fileDocument.GetLineIndex(entry);
        }

        throw new UnreachableException($"Unexpected document type: {document.GetType()}");
    }

    // Navigate between hunks

    public static int FindPreviousChangeBlock(this Document document, int lineIndex)
    {
        if (document is PatchDocument patchDocument)
        {
            var start = lineIndex;

            // Skip current block
            while (start >= 0 && patchDocument.Patch.Lines[start].Kind.IsAddedOrDeletedLine())
                start--;

            // Find next block
            while (start >= 0 && !patchDocument.Patch.Lines[start].Kind.IsAddedOrDeletedLine())
                start--;

            if (start < 0)
            {
                var hunkFirstLineIndex = lineIndex;
                while (hunkFirstLineIndex > 0 && patchDocument.Patch.Lines[hunkFirstLineIndex - 1].Kind.IsAddedOrDeletedLine())
                    hunkFirstLineIndex--;

                return hunkFirstLineIndex;
            }

            return start;
        }

        // Instead of doing nothing, just behave like going to the previous file
        if (document is FileDocument fileDocument)
        {
            var entryIndex = fileDocument.FindPreviousEntryIndex(lineIndex);
            return entryIndex < 0 ? lineIndex : fileDocument.GetLineIndex(entryIndex);
        }

        return lineIndex;
    }

    public static int FindNextChangeBlock(this Document document, int lineIndex)
    {
        if (document is PatchDocument patchDocument)
        {
            var end = lineIndex;

            // Skip current block
            while (end < patchDocument.Patch.Lines.Length && patchDocument.Patch.Lines[end].Kind.IsAddedOrDeletedLine())
                end++;

            // Find next block
            while (end < patchDocument.Patch.Lines.Length && !patchDocument.Patch.Lines[end].Kind.IsAddedOrDeletedLine())
                end++;

            if (end >= patchDocument.Patch.Lines.Length)
            {
                var hunkLastLineIndex = lineIndex;
                while (hunkLastLineIndex < patchDocument.Patch.Lines.Length - 1 && patchDocument.Patch.Lines[hunkLastLineIndex + 1].Kind.IsAddedOrDeletedLine())
                    hunkLastLineIndex++;

                return hunkLastLineIndex;
            }

            return end;
        }

        // Instead of doing nothing, just behave like going to the next file
        if (document is FileDocument fileDocument)
        {
            var entryIndex = fileDocument.FindNextEntryIndex(lineIndex);
            return entryIndex < 0 ? lineIndex : fileDocument.GetLineIndex(entryIndex);
        }

        return lineIndex;
    }
}
using GitIStage.Patching;
using GitIStage.UI;

namespace GitIStage.Patches;

internal sealed class PatchDocument : Document
{
    private PatchDocument(Patch patch, bool isStaged)
    {
        ThrowIfNull(patch);

        Patch = patch;
        IsStaged = isStaged;
        
        // TODO: This is super inefficient.
        Width = patch.Lines.Select(l => l.Text.ToString())
                           .DefaultIfEmpty(string.Empty)
                           .Max(t => t.LengthVisual());
    }

    public Patch Patch { get; }

    public bool IsStaged { get; }

    public override int Height => Patch.Lines.Length;

    public override int Width { get; }

    public override int EntryCount => Patch.Entries.Length;

    public override string GetLine(int index)
    {
        return Patch.Text.Lines[index].ToString();
    }

    public override int GetLineIndex(int index)
    {
        return Patch.Text.GetLineIndex(Patch.Entries[index].Span.Start);
    }

    public PatchEntry? FindEntry(int lineIndex)
    {
        var index = FindEntryIndex(lineIndex);
        return index < 0 ? null : Patch.Entries[index];
    }

    public override int FindEntryIndex(int lineIndex)
    {
        var lineSpan = Patch.Text.Lines[lineIndex].Span;
        
        // TODO: binary search would be more appropriate

        for (var i = 0; i < Patch.Entries.Length; i++)
        {
            var e = Patch.Entries[i];
            if (e.Span.OverlapsWith(lineSpan))
                return i;
        }

        return -1;
    }

    public static PatchDocument Create(string? patchText, bool isStaged)
    {
        var patch = Patch.Parse(patchText ?? string.Empty);
        return new PatchDocument(patch, isStaged);
    }
}
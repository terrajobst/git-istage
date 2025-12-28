using GitIStage.Patching;

namespace GitIStage.UI;

internal sealed class FileDocumentLineRenderer : ViewLineRenderer
{
    public new static FileDocumentLineRenderer Default { get; } = new();

    private static PatchEntry? GetChanges(View view, int lineIndex)
    {
        var document = view.Document as FileDocument;
        return document?.GetEntry(lineIndex);
    }

    private static ConsoleColor? GetForegroundColor(View view, int lineIndex)
    {
        var changes = GetChanges(view, lineIndex);
        if (changes is null)
            return null;

        switch (changes.ChangeKind)
        {
            case PatchEntryChangeKind.Added:
            case PatchEntryChangeKind.Copied:
                return ConsoleColor.DarkGreen;
            case PatchEntryChangeKind.Renamed:
            case PatchEntryChangeKind.Deleted:
            case PatchEntryChangeKind.Modified:
                return ConsoleColor.DarkRed;
            default:
                return null;
        }
    }

    private static ConsoleColor? GetBackgroundColor(View view, int lineIndex)
    {
        var foregroundColor = GetForegroundColor(view, lineIndex);

        var isSelected = view.SelectedLine == lineIndex;
        if (isSelected)
        {
            return foregroundColor != ConsoleColor.Gray
                ? ConsoleColor.Gray
                : ConsoleColor.DarkGray;
        }

        return null;
    }

    public override void Render(View view, int lineIndex)
    {
        var line = view.Document.GetLine(lineIndex);
        var foregroundColor = GetForegroundColor(view, lineIndex);
        var backgroundColor = GetBackgroundColor(view, lineIndex);

        RenderLine(view, lineIndex, line, foregroundColor, backgroundColor);
    }
}
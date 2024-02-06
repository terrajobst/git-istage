using LibGit2Sharp;

namespace GitIStage.UI;

internal sealed class FileDocumentLineRenderer : ViewLineRenderer
{
    public new static FileDocumentLineRenderer Default { get; } = new();

    private static TreeEntryChanges? GetChanges(View view, int lineIndex)
    {
        var document = view.Document as FileDocument;
        return document?.GetChange(lineIndex);
    }

    private static ConsoleColor? GetForegroundColor(View view, int lineIndex)
    {
        var changes = GetChanges(view, lineIndex);
        if (changes is null)
            return null;

        switch (changes.Status)
        {
            case ChangeKind.Added:
            case ChangeKind.Renamed:
                return ConsoleColor.DarkGreen;
            case ChangeKind.Deleted:
            case ChangeKind.Modified:
            case ChangeKind.Conflicted:
                return ConsoleColor.DarkRed;
            case ChangeKind.Unmodified:
            case ChangeKind.Copied:
            case ChangeKind.Ignored:
            case ChangeKind.Untracked:
            case ChangeKind.TypeChanged:
            case ChangeKind.Unreadable:
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
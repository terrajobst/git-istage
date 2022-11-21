using System;
using LibGit2Sharp;

namespace GitIStage;

internal sealed class FileDocumentLineRenderer : ViewLineRenderer
{
    public static new FileDocumentLineRenderer Default { get; } = new FileDocumentLineRenderer();
        
    private static TreeEntryChanges GetChanges(View view, int lineIndex)
    {
        var document = view.Document as FileDocument;
        return document?.GetChange(lineIndex);
    }

    private static ConsoleColor GetForegroundColor(View view, int lineIndex)
    {
        var changes = GetChanges(view, lineIndex);
        if (changes == null)
            return ConsoleColor.Gray;

        switch (changes.Status)
        {
            case ChangeKind.Added:
                return ConsoleColor.DarkGreen;
            case ChangeKind.Deleted:
            case ChangeKind.Modified:
            case ChangeKind.Renamed:
            case ChangeKind.Conflicted:
                return ConsoleColor.DarkRed;
            case ChangeKind.Unmodified:
            case ChangeKind.Copied:
            case ChangeKind.Ignored:
            case ChangeKind.Untracked:
            case ChangeKind.TypeChanged:
            case ChangeKind.Unreadable:
            default:
                return ConsoleColor.Gray;
        }
    }
    
    private static ConsoleColor GetBackgroundColor(View view, int lineIndex)
    {
        var foregroundColor = GetForegroundColor(view, lineIndex);

        var isSelected = view.SelectedLine == lineIndex;
        if (isSelected)
        {
            return foregroundColor != ConsoleColor.Gray
                ? ConsoleColor.Gray
                : ConsoleColor.DarkGray;
        }

        return ConsoleColor.Black;
    }

    public override void Render(View view, int lineIndex)
    {
        var line = view.Document.GetLine(lineIndex);
        var foregroundColor = GetForegroundColor(view, lineIndex);
        var backgroundColor = GetBackgroundColor(view, lineIndex);

        RenderLine(view, lineIndex, line, foregroundColor, backgroundColor);
    }
}
using GitIStage.Patching;
using GitIStage.UI;

namespace GitIStage.Patches;

internal sealed class PatchDocumentLineRenderer : ViewLineRenderer
{
    public new static PatchDocumentLineRenderer Default { get; } = new();

    private static PatchLine? GetLine(View view, int lineIndex)
    {
        var document = view.Document as PatchDocument;
        return document?.Patch.Lines[lineIndex];
    }

    private static ConsoleColor? GetForegroundColor(View view, int lineIndex)
    {
        var line = GetLine(view, lineIndex);
        if (line is null)
            return null;

        switch (line.Kind)
        {
            case PatchNodeKind.DiffGitHeader:
                return ConsoleColor.Yellow;
            case PatchNodeKind.OldPathHeader:
            case PatchNodeKind.NewPathHeader:
            case PatchNodeKind.OldModeHeader:
            case PatchNodeKind.NewModeHeader:
            case PatchNodeKind.DeletedFileModeHeader:
            case PatchNodeKind.NewFileModeHeader:
            case PatchNodeKind.CopyFromHeader:
            case PatchNodeKind.CopyToHeader:
            case PatchNodeKind.RenameFromHeader:
            case PatchNodeKind.RenameToHeader:
            case PatchNodeKind.SimilarityIndexHeader:
            case PatchNodeKind.DissimilarityIndexHeader:
            case PatchNodeKind.IndexHeader:
            case PatchNodeKind.UnknownHeader:
                return ConsoleColor.White;
            case PatchNodeKind.HunkHeader:
                return ConsoleColor.DarkCyan;
            case PatchNodeKind.ContextLine:
                goto default;
            case PatchNodeKind.AddedLine:
                return ConsoleColor.DarkGreen;
            case PatchNodeKind.DeletedLine:
                return ConsoleColor.DarkRed;
            default:
                return null;
        }
    }

    private static ConsoleColor? GetBackgroundColor(View view, int lineIndex)
    {
        var patchLine = GetLine(view, lineIndex);
        if (patchLine is null)
            return null;

        var kind = patchLine.Kind;

        var isSelected = view.SelectedLine == lineIndex;
        if (isSelected)
        {
            return kind is PatchNodeKind.AddedLine or PatchNodeKind.DeletedLine
                ? ConsoleColor.Gray
                : ConsoleColor.DarkGray;
        }

        return kind == PatchNodeKind.DiffGitHeader ? ConsoleColor.DarkBlue : null;
    }

    public override void Render(View view, int lineIndex)
    {
        var line = GetLine(view, lineIndex);
        if (line is null)
            return;

        var foregroundColor = GetForegroundColor(view, lineIndex);
        var backgroundColor = GetBackgroundColor(view, lineIndex);

        RenderLine(view, lineIndex, line.Text.ToString(), foregroundColor, backgroundColor);
    }
}
using GitIStage.UI;

namespace GitIStage.Patches;

internal sealed class PatchDocumentLineRenderer : ViewLineRenderer
{
    public new static PatchDocumentLineRenderer Default { get; } = new();

    private static PatchLine? GetLine(View view, int lineIndex)
    {
        var document = view.Document as PatchDocument;
        return document?.Lines[lineIndex];
    }

    private static ConsoleColor? GetForegroundColor(View view, int lineIndex)
    {
        var line = GetLine(view, lineIndex);
        if (line is null)
            return null;

        switch (line.Kind)
        {
            case PatchLineKind.DiffLine:
                return ConsoleColor.Yellow;
            case PatchLineKind.Header:
                return ConsoleColor.White;
            case PatchLineKind.Hunk:
                return ConsoleColor.DarkCyan;
            case PatchLineKind.Context:
                goto default;
            case PatchLineKind.Addition:
                return ConsoleColor.DarkGreen;
            case PatchLineKind.Removal:
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
            return kind.IsAdditionOrRemoval()
                ? ConsoleColor.Gray
                : ConsoleColor.DarkGray;
        }

        return kind == PatchLineKind.DiffLine ? ConsoleColor.DarkBlue : null;
    }

    public override void Render(View view, int lineIndex)
    {
        var line = GetLine(view, lineIndex);
        if (line is null)
            return;

        var foregroundColor = GetForegroundColor(view, lineIndex);
        var backgroundColor = GetBackgroundColor(view, lineIndex);

        RenderLine(view, lineIndex, line.Text, foregroundColor, backgroundColor);
    }
}
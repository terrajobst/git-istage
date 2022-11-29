using GitIStage.Services;
using GitIStage.UI;

namespace GitIStage.Patches;

internal sealed class PatchDocumentLineRenderer : ViewLineRenderer
{
    private readonly ColorService _colorService;

    public PatchDocumentLineRenderer(ColorService colorService)
    {
        _colorService = colorService;
    }

    private static PatchLine? GetLine(View view, int lineIndex)
    {
        var document = view.Document as PatchDocument;
        return document?.Lines[lineIndex];
    }

    private TextColor GetColor(PatchLine line)
    {
        switch (line.Kind)
        {
            case PatchLineKind.DiffLine:
                return new TextColor(ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            case PatchLineKind.Header:
                return _colorService.DiffMeta;
            case PatchLineKind.Hunk:
                return _colorService.DiffHunk;
            case PatchLineKind.Addition:
                return _colorService.DiffNew;
            case PatchLineKind.Removal:
                return _colorService.DiffOld;
            case PatchLineKind.Context:
            case PatchLineKind.NoEndOfLine:
            default:
                return _colorService.DiffContext;
        }
    }

    public override void Render(View view, int lineIndex)
    {
        var line = GetLine(view, lineIndex);
        if (line is null)
            return;

        var textColor = GetColor(line);

        var foregroundColor = textColor.Foreground ?? ConsoleColor.DarkGray;
        var backgroundColor = textColor.Background ?? ConsoleColor.Black;

        if (lineIndex == view.SelectedLine)
            backgroundColor = ConsoleColor.DarkGray;

        RenderLine(view, lineIndex, line.Text, foregroundColor, backgroundColor);
    }
}
using GitIStage.Patches;
using GitIStage.Text;

namespace GitIStage.UI;

// TODO: We should rethink PatchDocument and FileDocument.
//       It seems both should have their text stored in SourceText. Rendering shouldn't create new strings
//       but should simply use ReadOnlySpan<char> when calling Console.Write().
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

    public override string GetLine(int index)
    {
        var span = Patch.Lines[index].Span;
        return Patch.Text.ToString(span);
    }

    public override IEnumerable<StyledSpan> GetLineStyles(int index)
    {
        var line = Patch.Lines[index];
        var foreground = GetForegroundColor(line);
        var background = GetBackgroundColor(line);

        // NOTE: We're expected return spans that start at this line.
        var span = new TextSpan(0, line.Span.Length);

        return [new StyledSpan(span, foreground, background)];
    }

    private static ConsoleColor? GetForegroundColor(PatchLine line)
    {
        switch (line.Kind)
        {
            case PatchNodeKind.DiffGitHeader:
                return ConsoleColor.White;
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

    private static ConsoleColor? GetBackgroundColor(PatchLine line)
    {
        return line.Kind == PatchNodeKind.DiffGitHeader
            ? ConsoleColor.DarkCyan
            : null;
    }

    public static PatchDocument Create(string? patchText, bool isStaged)
    {
        var patch = Patch.Parse(patchText ?? string.Empty);
        return new PatchDocument(patch, isStaged);
    }
}
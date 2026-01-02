using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class PatchDocument : Document
{
    private PatchDocument(Patch patch)
        : base(patch.Text)
    {
        ThrowIfNull(patch);

        Patch = patch;
    }

    public Patch Patch { get; }

    public override IEnumerable<StyledSpan> GetLineStyles(int index)
    {
        // NOTE: We're expected return spans that start at this line.

        var line = Patch.Lines[index];
        var lineForeground = GetLineForegroundColor(line);

        if (lineForeground is not null)
        {
            var lineBackground = GetLineBackgroundColor(line);
            var span = new TextSpan(0, line.Span.Length);
            return [new StyledSpan(span, lineForeground, lineBackground)];
        }

        return line.DescendantsAndSelf()
                   .OfType<PatchToken>()
                   .Select(GetSpan);
    }

    private static ConsoleColor? GetLineForegroundColor(PatchLine line)
    {
        switch (line.Kind)
        {
            case PatchNodeKind.EntryHeader:
                return ConsoleColor.White;
            case PatchNodeKind.AddedLine:
                return ConsoleColor.DarkGreen;
            case PatchNodeKind.DeletedLine:
                return ConsoleColor.DarkRed;
            default:
                return null;
        }
    }

    private static ConsoleColor? GetLineBackgroundColor(PatchLine line)
    {
        return line.Kind == PatchNodeKind.EntryHeader
            ? ConsoleColor.DarkCyan
            : null;
    }

    public static PatchDocument Create(Patch patch)
    {
        return new PatchDocument(patch);
    }

    private static StyledSpan GetSpan(PatchToken token)
    {
        var foreground = token.Kind switch
        {
            PatchNodeKind.PathToken => ConsoleColor.White,
            PatchNodeKind.HashToken => ConsoleColor.DarkYellow,
            PatchNodeKind.ModeToken => ConsoleColor.DarkYellow,
            PatchNodeKind.TextToken => ConsoleColor.Gray,
            PatchNodeKind.PercentageToken => ConsoleColor.DarkMagenta,
            PatchNodeKind.RangeToken => ConsoleColor.Cyan,
            PatchNodeKind.MinusMinusMinusToken => ConsoleColor.DarkRed,
            PatchNodeKind.PlusPlusPlusToken => ConsoleColor.Green,
            _ => token.Kind.IsKeyword()
                ? ConsoleColor.Cyan
                : token.Kind.IsOperator()
                    ? ConsoleColor.DarkCyan
                    : throw new UnreachableException($"Unexpected token kind {token.Kind}")
        };

        var line = token.Ancestors().OfType<PatchLine>().First();
        var lineStart = line.TextLine.Start;
        var start = token.Span.Start - lineStart;
        var end = token.Span.End - lineStart;
        var span = TextSpan.FromBounds(start, end);
        return new StyledSpan(span, foreground, null);
    }
}
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

    private static TextColor? GetLineForegroundColor(PatchLine line)
    {
        switch (line.Kind)
        {
            case PatchNodeKind.EntryHeader:
                return Colors.EntryHeaderForeground;
            case PatchNodeKind.AddedLine:
                return Colors.AddedText;
            case PatchNodeKind.DeletedLine:
                return Colors.DeletedText;
            default:
                return null;
        }
    }

    private static TextColor? GetLineBackgroundColor(PatchLine line)
    {
        return line.Kind == PatchNodeKind.EntryHeader
            ? Colors.EntryHeaderBackground
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
            PatchNodeKind.PathToken => Colors.PathTokenForeground,
            PatchNodeKind.HashToken => Colors.HashTokenForeground,
            PatchNodeKind.ModeToken => Colors.ModeTokenForeground,
            PatchNodeKind.TextToken => Colors.TextTokenForeground,
            PatchNodeKind.PercentageToken => Colors.PercentageTokenForeground,
            PatchNodeKind.RangeToken => Colors.RangeTokenForeground,
            PatchNodeKind.MinusMinusMinusToken => Colors.MinusMinusMinusTokenForeground,
            PatchNodeKind.PlusPlusPlusToken => Colors.PlusPlusPlusTokenForeground,
            _ => token.Kind.IsKeyword()
                ? Colors.KeywordForeground
                : token.Kind.IsOperator()
                    ? Colors.OperatorForeground
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
using System.Collections.Frozen;
using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class PatchDocument : Document
{
    public PatchDocument(Patch patch, FrozenDictionary<PatchEntry, LineHighlights> highlights)
        : base(patch.Text)
    {
        Patch = patch;
        Highlights = highlights;
    }

    public Patch Patch { get; }

    public FrozenDictionary<PatchEntry, LineHighlights> Highlights { get; }

    public override void GetLineStyles(int index, List<StyledSpan> receiver)
    {
        var line = Patch.Lines[index];
        var lineForeground = GetLineForegroundColor(line);
        var lineBackground = lineForeground?.Lerp(TextColor.Black, 0.8f);

        if (line is PatchHunkLine hunkLine)
        {
            var entry = hunkLine.Ancestors().OfType<PatchEntry>().First();
            var hasSyntaxColoring = Highlights.ContainsKey(entry);

            // Line-level background style (zero-length sentinel span)
            if (lineBackground is not null)
            {
                receiver.Add(new StyledSpan(new TextSpan(0, 0), new TextStyle
                {
                    Foreground = hasSyntaxColoring ? null : lineForeground,
                    Background = lineBackground
                }));
            }

            var startLine = entry.Hunks.First().Lines.First().LineIndex;

            if (hunkLine.Span.Length > 0)
            {
                var modifierStyle = new TextStyle { Foreground = lineForeground };
                receiver.Add(new StyledSpan(new TextSpan(0, 1), modifierStyle));
            }

            if (Highlights.TryGetValue(entry, out var lineHighlights))
            {
                var highlightLineIndex = index - startLine;
                if (highlightLineIndex >= 0 && highlightLineIndex < lineHighlights.Count)
                    receiver.AddRange(lineHighlights[highlightLineIndex].AsSpan());
            }
        }
        else
        {
            // Line-level style for non-hunk lines (e.g. entry headers)
            if (lineForeground is not null || lineBackground is not null)
            {
                receiver.Add(new StyledSpan(new TextSpan(0, 0), new TextStyle
                {
                    Foreground = lineForeground,
                    Background = lineBackground
                }));
            }

            var styledTokens = line
                .Descendants()
                .OfType<PatchToken>()
                .Select(ToStyledSpan);
            receiver.AddRange(styledTokens);
        }
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

    private static StyledSpan ToStyledSpan(PatchToken token)
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

        var text = token.Root.Text;
        var lineIndex = text.GetLineIndex(token.Span.Start);
        var lineStart = text.Lines[lineIndex].Start;
        return new StyledSpan(token.Span.RelativeTo(lineStart), foreground, null);
    }
}

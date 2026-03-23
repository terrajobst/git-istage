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

    public override void GetLineStyles(int index, List<ClassifiedSpan> receiver)
    {
        var line = Patch.Lines[index];
        var lineClassification = GetLineClassification(line);

        if (line is PatchHunkLine hunkLine)
        {
            var entry = hunkLine.Ancestors().OfType<PatchEntry>().First();
            var hasSyntaxColoring = Highlights.ContainsKey(entry);

            var sentinelClassification = GetSentinelClassification(lineClassification, hasSyntaxColoring);
            if (sentinelClassification is not null)
                receiver.Add(new ClassifiedSpan(new TextSpan(0, 0), sentinelClassification));

            var startLine = entry.Hunks.First().Lines.First().LineIndex;

            if (hunkLine.Span.Length > 0)
            {
                var modifierClassification = GetModifierClassification(lineClassification);
                if (modifierClassification is not null)
                    receiver.Add(new ClassifiedSpan(new TextSpan(0, 1), modifierClassification));
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
            if (lineClassification is not null)
                receiver.Add(new ClassifiedSpan(new TextSpan(0, 0), lineClassification));

            var classifiedTokens = line
                .Descendants()
                .OfType<PatchToken>()
                .Select(ToClassifiedSpan);
            receiver.AddRange(classifiedTokens);
        }
    }

    private static Classification? GetLineClassification(PatchLine line)
    {
        return line.Kind switch
        {
            PatchNodeKind.EntryHeader => PatchClassification.EntryHeader,
            PatchNodeKind.AddedLine => PatchClassification.AddedLine,
            PatchNodeKind.DeletedLine => PatchClassification.DeletedLine,
            _ => null
        };
    }

    private static Classification? GetSentinelClassification(Classification? lineClassification, bool hasSyntaxColoring)
    {
        if (lineClassification is null)
            return null;

        if (!hasSyntaxColoring)
            return lineClassification;

        if (lineClassification == PatchClassification.AddedLine)
            return PatchClassification.AddedLineBackground;
        if (lineClassification == PatchClassification.DeletedLine)
            return PatchClassification.DeletedLineBackground;

        return lineClassification;
    }

    private static Classification? GetModifierClassification(Classification? lineClassification)
    {
        if (lineClassification == PatchClassification.AddedLine)
            return PatchClassification.AddedLineModifier;
        if (lineClassification == PatchClassification.DeletedLine)
            return PatchClassification.DeletedLineModifier;

        return null;
    }

    private static ClassifiedSpan ToClassifiedSpan(PatchToken token)
    {
        var classification = token.Kind switch
        {
            PatchNodeKind.PathToken => PatchClassification.PathToken,
            PatchNodeKind.HashToken => PatchClassification.HashToken,
            PatchNodeKind.ModeToken => PatchClassification.ModeToken,
            PatchNodeKind.TextToken => PatchClassification.TextToken,
            PatchNodeKind.PercentageToken => PatchClassification.PercentageToken,
            PatchNodeKind.RangeToken => PatchClassification.RangeToken,
            PatchNodeKind.MinusMinusMinusToken => PatchClassification.MinusMinusMinusToken,
            PatchNodeKind.PlusPlusPlusToken => PatchClassification.PlusPlusPlusToken,
            _ => token.Kind.IsKeyword()
                ? PatchClassification.Keyword
                : token.Kind.IsOperator()
                    ? PatchClassification.Operator
                    : throw new UnreachableException($"Unexpected token kind {token.Kind}")
        };

        var text = token.Root.Text;
        var lineIndex = text.GetLineIndex(token.Span.Start);
        var lineStart = text.Lines[lineIndex].Start;
        return new ClassifiedSpan(token.Span.RelativeTo(lineStart), classification);
    }
}

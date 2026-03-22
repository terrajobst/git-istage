using System.Collections.Immutable;
using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.Text;
using TextMateSharp.Themes;
using IGrammar = TextMateSharp.Grammars.IGrammar;
using IStateStack = TextMateSharp.Grammars.IStateStack;
using StateStack = TextMateSharp.Grammars.StateStack;

namespace GitIStage.Services;

public sealed class LineHighlights
{
    public static LineHighlights Empty { get; } = new(ImmutableArray<ImmutableArray<StyledSpan>>.Empty);

    private readonly ImmutableArray<ImmutableArray<StyledSpan>> _lineStyles;

    private LineHighlights(ImmutableArray<ImmutableArray<StyledSpan>> lineStyles)
    {
        _lineStyles = lineStyles;
    }

    public static LineHighlights Create(SourceText text, IReadOnlyCollection<StyledSpan> spans, int offset = 0)
    {
        var start = offset;
        var end = spans.Select(s => s.Span.End).DefaultIfEmpty(offset).Last();
        var startLine = text.GetLineIndex(start);
        var endLine = text.GetLineIndex(end);
        var lineCount = endLine - startLine + 1;

        var lineBuilder = ImmutableArray.CreateBuilder<ImmutableArray<StyledSpan>>(lineCount);
        for (var i = 0; i < lineCount; i++)
            lineBuilder.Add(ImmutableArray<StyledSpan>.Empty);

        var styleBuilder = ImmutableArray.CreateBuilder<StyledSpan>();
        var previousIndex = -1;

        foreach (var styledSpan in spans)
        {
            var style = styledSpan.Style;
            var span = styledSpan.Span;
            var originalLineIndex = text.GetLineIndex(span.Start);
            var line = text.Lines[originalLineIndex];
            var index = originalLineIndex - startLine;

            if (previousIndex >= 0 && index != previousIndex)
            {
                CommitLine(previousIndex, lineBuilder, styleBuilder);
            }

            var adjustedSpan = new TextSpan(span.Start - line.Start, span.Length);
            var adjustedStyledSpan = new StyledSpan(adjustedSpan, style);
            styleBuilder.Add(adjustedStyledSpan);

            previousIndex = index;
        }

        CommitLine(previousIndex, lineBuilder, styleBuilder);
        return new LineHighlights(lineBuilder.ToImmutable());

        static void CommitLine(
            int index,
            ImmutableArray<ImmutableArray<StyledSpan>>.Builder lineBuilder,
            ImmutableArray<StyledSpan>.Builder styleBuilder)
        {
            if (styleBuilder.Count == 0)
                return;

            lineBuilder[index] = styleBuilder.ToImmutable();
            styleBuilder.Clear();
        }
    }

    public static LineHighlights ForPatchEntry(TextLinesWithStates original, PatchEntry patch, IGrammar grammar)
    {
        if (patch.Hunks.Length == 0)
            return Empty;

        var receiver = new List<StyledSpan>();
        var text = patch.Root.Text;
        var reHighlightOldLineStart = -1;
        var newState = (IStateStack?)null;

        foreach (var hunk in patch.Hunks)
        {
            // old states will always have at least one line. And the first
            // state is always null, hence we can fold -1 and 0 both to 0.
            var oldLine = int.Max(0, hunk.OldRange.LineNumber - 1);

            if (reHighlightOldLineStart < 0)
            {
                newState = oldLine < original.States.Length
                    ? original.States[oldLine]
                    : null;
            }
            else
            {
                Debug.Assert(newState is not null);

                for (var line = reHighlightOldLineStart; line < oldLine; line++)
                {
                    var oldLineMemory = original.Lines[line];
                    GetHighlightState(grammar, oldLineMemory, ref newState);
                }
            }

            foreach (var hunkLine in hunk.Lines)
            {
                var lineStart = hunkLine.Span.Start + 1;
                var lineSpan = TextSpan.FromBounds(lineStart, hunkLine.Span.End);
                var lineText = text.AsMemory(lineSpan);

                if (hunkLine.Kind == PatchNodeKind.ContextLine)
                {
                    GetHighlights(grammar, lineStart, lineText, ref newState, receiver);
                    oldLine++;
                }
                else if (hunkLine.Kind == PatchNodeKind.AddedLine)
                {
                    GetHighlights(grammar, lineStart, lineText, ref newState, receiver);
                }
                else if (hunkLine.Kind == PatchNodeKind.DeletedLine)
                {
                    GetHighlights(grammar, lineStart, lineText, original.States[oldLine], receiver);
                }
            }

            if (oldLine < original.States.Length)
                reHighlightOldLineStart = SameState(newState, original.States[oldLine]) ? -1 : oldLine;
        }

        var offset = patch.Hunks.First().Lines.First().Span.Start;
        return Create(patch.Root.Text, receiver, offset);
    }

    private static bool SameState(IStateStack? a, IStateStack? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        var stateA = (StateStack)a;
        var stateB = (StateStack)b;

        return stateA.RuleId == stateB.RuleId &&
                stateA.Depth == stateB.Depth &&
                stateA.EndRule == stateB.EndRule &&
                stateA.BeginRuleCapturedEOL == stateB.BeginRuleCapturedEOL &&
                SameState(stateA.Parent, stateB.Parent);
    }

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, IStateStack? state, ICollection<StyledSpan> receiver)
    {
        GetHighlights(grammar, lineStart, text, ref state, receiver);
    }

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, ref IStateStack? state, ICollection<StyledSpan> receiver)
    {
        var tokenizedLine = grammar.TokenizeLine(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;

        foreach (var token in tokenizedLine.Tokens)
        {
            var startIndex = token.StartIndex > text.Length ? text.Length : token.StartIndex;
            var endIndex = token.EndIndex > text.Length ? text.Length : token.EndIndex;

            var foreground = -1;
            var background = -1;
            var fontStyle = FontStyle.None;

            foreach (var themeRule in SyntaxTheme.Instance.Theme.Match(token.Scopes))
            {
                if (foreground == -1 && themeRule.foreground > 0)
                    foreground = themeRule.foreground;

                if (background == -1 && themeRule.background > 0)
                    background = themeRule.background;

                if (fontStyle == FontStyle.None && themeRule.fontStyle != FontStyle.None)
                    fontStyle = themeRule.fontStyle;
            }

            var style = new TextStyle
            {
                Foreground = GetColor(foreground),
                Background = GetColor(background),
                Attributes = GetAttributes(fontStyle)
            };

            var span = new TextSpan(lineStart + startIndex, endIndex - startIndex);
            var styledSpan = new StyledSpan(span, style);
            receiver.Add(styledSpan);
        }
    }

    private static void GetHighlightState(IGrammar grammar, ReadOnlyMemory<char> text, ref IStateStack? state)
    {
        // Note: we use TokenizeLine2 to avoid the overhead of creating tokens
        var tokenizedLine = grammar.TokenizeLine2(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;
    }

    private static TextColor? GetColor(int colorId)
    {
        if (colorId == -1)
            return null;

        return TextColor.FromHex(SyntaxTheme.Instance.Theme.GetColor(colorId));
    }

    private static TextAttributes GetAttributes(FontStyle fontStyle)
    {
        var result = TextAttributes.None;

        if (fontStyle == FontStyle.NotSet)
            return result;

        if ((fontStyle & FontStyle.Italic) != 0)
            result |= TextAttributes.Italic;

        if ((fontStyle & FontStyle.Bold) != 0)
            result |= TextAttributes.Bold;

        if ((fontStyle & FontStyle.Underline) != 0)
            result |= TextAttributes.Underline;

        if ((fontStyle & FontStyle.Strikethrough) != 0)
            result |= TextAttributes.Strike;

        return result;
    }

    public ImmutableArray<StyledSpan> this[int index] => _lineStyles[index];

    public int Count => _lineStyles.Length;
}

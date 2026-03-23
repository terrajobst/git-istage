using System.Collections.Immutable;
using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.Text;
using IGrammar = TextMateSharp.Grammars.IGrammar;
using IStateStack = TextMateSharp.Grammars.IStateStack;
using StateStack = TextMateSharp.Grammars.StateStack;

namespace GitIStage.Services;

public sealed class LineHighlights
{
    public static LineHighlights Empty { get; } = new(ImmutableArray<ImmutableArray<ClassifiedSpan>>.Empty);

    private readonly ImmutableArray<ImmutableArray<ClassifiedSpan>> _lineStyles;

    private LineHighlights(ImmutableArray<ImmutableArray<ClassifiedSpan>> lineStyles)
    {
        _lineStyles = lineStyles;
    }

    public static LineHighlights Create(SourceText text, IReadOnlyCollection<ClassifiedSpan> spans, int offset = 0)
    {
        var start = offset;
        var end = spans.Select(s => s.Span.End).DefaultIfEmpty(offset).Last();
        var startLine = text.GetLineIndex(start);
        var endLine = text.GetLineIndex(end);
        var lineCount = endLine - startLine + 1;

        var lineBuilder = ImmutableArray.CreateBuilder<ImmutableArray<ClassifiedSpan>>(lineCount);
        for (var i = 0; i < lineCount; i++)
            lineBuilder.Add(ImmutableArray<ClassifiedSpan>.Empty);

        var styleBuilder = ImmutableArray.CreateBuilder<ClassifiedSpan>();
        var previousIndex = -1;

        foreach (var classifiedSpan in spans)
        {
            var span = classifiedSpan.Span;
            var originalLineIndex = text.GetLineIndex(span.Start);
            var line = text.Lines[originalLineIndex];
            var index = originalLineIndex - startLine;

            if (previousIndex >= 0 && index != previousIndex)
            {
                CommitLine(previousIndex, lineBuilder, styleBuilder);
            }

            var adjustedSpan = new TextSpan(span.Start - line.Start, span.Length);
            var adjusted = new ClassifiedSpan(adjustedSpan, classifiedSpan.Classification);
            styleBuilder.Add(adjusted);

            previousIndex = index;
        }

        CommitLine(previousIndex, lineBuilder, styleBuilder);
        return new LineHighlights(lineBuilder.ToImmutable());

        static void CommitLine(
            int index,
            ImmutableArray<ImmutableArray<ClassifiedSpan>>.Builder lineBuilder,
            ImmutableArray<ClassifiedSpan>.Builder styleBuilder)
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

        var receiver = new List<ClassifiedSpan>();

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

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, IStateStack? state, List<ClassifiedSpan> receiver)
    {
        GetHighlights(grammar, lineStart, text, ref state, receiver);
    }

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, ref IStateStack? state, List<ClassifiedSpan> receiver)
    {
        var tokenizedLine = grammar.TokenizeLine(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;

        foreach (var token in tokenizedLine.Tokens)
        {
            var startIndex = token.StartIndex > text.Length ? text.Length : token.StartIndex;
            var endIndex = token.EndIndex > text.Length ? text.Length : token.EndIndex;

            var span = new TextSpan(lineStart + startIndex, endIndex - startIndex);
            var classification = Classification.Create(token.Scopes);
            receiver.Add(new ClassifiedSpan(span, classification));
        }
    }

    private static void GetHighlightState(IGrammar grammar, ReadOnlyMemory<char> text, ref IStateStack? state)
    {
        // Note: we use TokenizeLine2 to avoid the overhead of creating tokens
        var tokenizedLine = grammar.TokenizeLine2(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;
    }

    public ImmutableArray<ClassifiedSpan> this[int index] => _lineStyles[index];

    public int Count => _lineStyles.Length;
}

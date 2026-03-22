using System.Collections.Immutable;
using IGrammar = TextMateSharp.Grammars.IGrammar;
using IStateStack = TextMateSharp.Grammars.IStateStack;

namespace GitIStage.Services;

public sealed class TextLinesWithStates
{
    public static TextLinesWithStates Create(TextLines lines, IGrammar grammar)
    {
        var states = new List<IStateStack?>(lines.Count);
        var state = (IStateStack?)null;

        foreach (var line in lines)
        {
            states.Add(state);
            var lineResult = grammar.TokenizeLine2(line, state, Timeout.InfiniteTimeSpan);
            state = lineResult.RuleStack;
        }

        return new TextLinesWithStates(lines, [.. states]);
    }

    private TextLinesWithStates(TextLines lines, ImmutableArray<IStateStack?> states)
    {
        Lines = lines;
        States = states;
    }

    public TextLines Lines { get; }

    public ImmutableArray<IStateStack?> States { get; }
}

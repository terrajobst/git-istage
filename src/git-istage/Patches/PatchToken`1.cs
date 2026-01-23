using System.Diagnostics;
using GitIStage.Text;

namespace GitIStage.Patches;

public sealed class PatchToken<T> : PatchToken
{
    internal PatchToken(Patch root,
                        PatchNodeKind kind,
                        TextSpan span,
                        T value,
                        PatchTrivia? trailingWhitespace)
        : base(root, kind, span, trailingWhitespace)
    {
        Value = value;
    }

    public new T Value { get; }

    private protected override object? ValueCore => Value;

    internal override PatchToken<T> WithTrailingWhitespace(PatchTrivia trivia)
    {
        Debug.Assert(TrailingWhitespace is null);
        return new PatchToken<T>(Root, Kind, Span, Value, trivia);
    }
}
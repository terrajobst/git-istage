using System.Diagnostics;
using GitIStage.Text;

namespace GitIStage.Patches;

public abstract partial class PatchToken
{
    private sealed class NoValue : PatchToken
    {
        public NoValue(Patch root,
                       PatchNodeKind kind,
                       TextSpan span,
                       PatchTrivia? trailingWhitespace)
            : base(root, kind, span, trailingWhitespace)
        {
        }

        private protected override object? ValueCore => null;

        internal override PatchToken WithTrailingWhitespace(PatchTrivia trivia)
        {
            Debug.Assert(TrailingWhitespace is null);
            return new NoValue(Root, Kind, Span, trivia);
        }
    }
}
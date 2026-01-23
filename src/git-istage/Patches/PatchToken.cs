using GitIStage.Text;

namespace GitIStage.Patches;

public abstract partial class PatchToken : PatchNode
{
    internal static PatchToken Create(Patch root,
                                      PatchNodeKind kind,
                                      TextSpan span,
                                      PatchTrivia? trailingWhitespace)
    {
        ThrowIfNull(root);

        return new NoValue(root, kind, span, trailingWhitespace);
    }

    internal static PatchToken<T> Create<T>(Patch root,
                                            PatchNodeKind kind,
                                            TextSpan span,
                                            T value,
                                            PatchTrivia? trailingWhitespace)
    {
        ThrowIfNull(root);

        return new PatchToken<T>(root, kind, span, value, trailingWhitespace);
    }

    private protected PatchToken(Patch root,
                                 PatchNodeKind kind,
                                 TextSpan span,
                                 PatchTrivia? trailingWhitespace)
    {
        ThrowIfNull(root);

        Root = root;
        Kind = kind;
        Span = span;
        TrailingWhitespace = trailingWhitespace;
    }

    public sealed override Patch Root { get; }

    public override PatchNodeKind Kind { get; }

    public override TextSpan Span { get; }

    public override TextSpan FullSpan
    {
        get
        {
            var start = Span.Start;
            var end = TrailingWhitespace?.Span.End ?? Span.End;
            return TextSpan.FromBounds(start, end);
        }
    }

    public PatchTrivia? TrailingWhitespace { get; }

    public object? Value => ValueCore;

    private protected abstract object? ValueCore { get; }

    internal abstract PatchToken WithTrailingWhitespace(PatchTrivia trivia);

    public sealed override IEnumerable<PatchNode> Children() => [];
}
using GitIStage.Text;

namespace GitIStage.Patches;

public sealed class PatchTrivia : PatchNode
{
    internal PatchTrivia(Patch root,
                         PatchNodeKind kind,
                         TextSpan span)
    {
        ThrowIfNull(root);

        Root = root;
        Kind = kind;
        Span = span;
    }

    public override Patch Root { get; }

    public override PatchNodeKind Kind { get; }

    public override TextSpan Span { get; }

    public override TextSpan FullSpan => Span;

    public override IEnumerable<PatchNode> Children() => [];
}
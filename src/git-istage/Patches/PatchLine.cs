using GitIStage.Text;

namespace GitIStage.Patches;

public abstract class PatchLine : PatchNode
{
    private protected PatchLine(Patch root, TextLine textLine)
    {
        ThrowIfNull(root);
        ThrowIfNull(textLine);

        Root = root;
        TextLine = textLine;
    }

    public override TextSpan Span => TextLine.Span;

    public override Patch Root { get; }

    public TextLine TextLine { get; }

    public ReadOnlySpan<char> Text => Root.Text.AsSpan(TextLine.Span);

    public ReadOnlySpan<char> LineBreak => Root.Text.AsSpan(TextLine.SpanLineBreak);

    public override IEnumerable<PatchNode> Children => [];
}
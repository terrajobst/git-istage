using GitIStage.Patching.Text;

namespace GitIStage.Patching;

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

    public ReadOnlySpan<char> Text => Root.Text.ToString().AsSpan(TextLine.Start, TextLine.Length);

    public ReadOnlySpan<char> LineBreak => Root.Text.ToString().AsSpan(TextLine.LineBreakSpan.Start, TextLine.LineBreakSpan.Length);

    public override IEnumerable<PatchNode> Children => [];
}
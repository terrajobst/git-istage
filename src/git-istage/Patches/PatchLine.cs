using GitIStage.Text;

namespace GitIStage.Patches;

public abstract class PatchLine : PatchNode
{
    private protected PatchLine(Patch root)
    {
        ThrowIfNull(root);
        ThrowIfNull(root);

        Root = root;
    }

    public int LineIndex => Root.Text.GetLineIndex(Span.Start);

    public TextLine TextLine => Root.Text.Lines[LineIndex];

    public ReadOnlySpan<char> Text => Root.Text.AsSpan(TextLine.Span);

    public ReadOnlySpan<char> LineBreak => Root.Text.AsSpan(TextLine.SpanLineBreak);

    public sealed override Patch Root { get; }
}
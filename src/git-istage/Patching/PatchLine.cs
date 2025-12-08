using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public abstract class PatchLine : PatchNode
{
    private protected PatchLine(TextLine textLine)
    {
        ThrowIfNull(textLine);

        TextLine = textLine;
    }

    public override TextSpan Span => TextLine.Span;

    public TextLine TextLine { get; }

    public override IEnumerable<PatchNode> Children => [];
}
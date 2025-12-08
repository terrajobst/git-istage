using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public sealed class PatchHunkLine : PatchLine
{
    internal PatchHunkLine(PatchNodeKind kind, TextLine textLine)
        : base(textLine)
    {
        Kind = kind;
    }

    public override PatchNodeKind Kind { get; }
}
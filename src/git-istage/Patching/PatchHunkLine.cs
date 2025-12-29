using GitIStage.Text;

namespace GitIStage.Patching;

public sealed class PatchHunkLine : PatchLine
{
    internal PatchHunkLine(Patch patch, PatchNodeKind kind, TextLine textLine)
        : base(patch, textLine)
    {
        Kind = kind;
    }

    public override PatchNodeKind Kind { get; }
}
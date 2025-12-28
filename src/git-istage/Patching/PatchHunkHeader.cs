using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public sealed class PatchHunkHeader : PatchLine
{
    internal PatchHunkHeader(Patch root,
                             TextLine textLine,
                             int oldLine,
                             int oldCount,
                             int newLine,
                             int newCount,
                             string function)
        : base(root, textLine)
    {
        ThrowIfNegative(oldLine);
        ThrowIfNegative(oldCount);
        ThrowIfNegative(newLine);
        ThrowIfNegative(newCount);
        ThrowIfNull(function);

        OldLine = oldLine;
        OldCount = oldCount;
        NewLine = newLine;
        NewCount = newCount;
        Function = function;
    }

    public override PatchNodeKind Kind => PatchNodeKind.HunkHeader;

    public int OldLine { get; }

    public int OldCount { get; }

    public int NewLine { get; }

    public int NewCount { get; }

    public string Function { get; set; }
}
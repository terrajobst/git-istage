using GitIStage.Text;

namespace GitIStage.Patching;

public abstract class PatchEntryHeader : PatchLine
{
    private protected PatchEntryHeader(Patch root, TextLine textLine)
        : base(root, textLine)
    {
    }
}
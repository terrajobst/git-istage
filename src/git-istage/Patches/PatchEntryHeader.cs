using GitIStage.Text;

namespace GitIStage.Patches;

public abstract class PatchEntryHeader : PatchLine
{
    private protected PatchEntryHeader(Patch root, TextLine textLine)
        : base(root, textLine)
    {
    }
}
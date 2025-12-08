using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public abstract class PatchEntryHeader : PatchLine
{
    private protected PatchEntryHeader(TextLine textLine)
        : base(textLine)
    {
    }
}
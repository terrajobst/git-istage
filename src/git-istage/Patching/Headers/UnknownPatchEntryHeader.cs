using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class UnknownPatchEntryHeader : PatchEntryHeader
{
    internal UnknownPatchEntryHeader(TextLine line)
        : base(line)
    {
    }

    public override PatchNodeKind Kind => PatchNodeKind.UnknownHeader;
}
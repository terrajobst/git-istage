using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class UnknownPatchEntryHeader : PatchEntryHeader
{
    internal UnknownPatchEntryHeader(Patch root, TextLine line)
        : base(root, line)
    {
    }

    public override PatchNodeKind Kind => PatchNodeKind.UnknownHeader;
}
using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class UnknownPatchEntryHeader : PatchEntryHeader
{
    internal UnknownPatchEntryHeader(Patch root, TextLine line)
        : base(root, line)
    {
    }

    public override PatchNodeKind Kind => PatchNodeKind.UnknownHeader;
}
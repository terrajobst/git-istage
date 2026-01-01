using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class OldModePatchEntryHeader : PatchEntryHeader
{
    internal OldModePatchEntryHeader(Patch root, TextLine line, PatchEntryMode mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldModeHeader;

    public PatchEntryMode Mode { get; }
}
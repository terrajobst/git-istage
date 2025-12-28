using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class OldModePatchEntryHeader : PatchEntryHeader
{
    internal OldModePatchEntryHeader(Patch root, TextLine line, int mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldModeHeader;

    public int Mode { get; }
}
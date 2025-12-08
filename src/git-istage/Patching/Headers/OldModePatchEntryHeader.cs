using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class OldModePatchEntryHeader : PatchEntryHeader
{
    internal OldModePatchEntryHeader(TextLine line, int mode)
        : base(line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldModeHeader;

    public int Mode { get; }
}
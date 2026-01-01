using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class NewModePatchEntryHeader : PatchEntryHeader
{
    internal NewModePatchEntryHeader(Patch root, TextLine line, PatchEntryMode mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewModeHeader;

    public PatchEntryMode Mode { get; }
}
using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class NewFileModePatchEntryHeader : PatchEntryHeader
{
    internal NewFileModePatchEntryHeader(Patch root, TextLine line, PatchEntryMode mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewFileModeHeader;

    public PatchEntryMode Mode { get; }
}
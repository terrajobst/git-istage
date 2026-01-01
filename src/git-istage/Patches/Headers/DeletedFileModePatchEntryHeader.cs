using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class DeletedFileModePatchEntryHeader : PatchEntryHeader
{
    internal DeletedFileModePatchEntryHeader(Patch root, TextLine line, PatchEntryMode mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DeletedFileModeHeader;

    public PatchEntryMode Mode { get; }
}
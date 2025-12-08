using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class DeletedFileModePatchEntryHeader : PatchEntryHeader
{
    internal DeletedFileModePatchEntryHeader(TextLine line, int mode)
        : base(line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DeletedFileModeHeader;

    public int Mode { get; }
}
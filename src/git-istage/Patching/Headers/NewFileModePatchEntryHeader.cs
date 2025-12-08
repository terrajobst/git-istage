using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewFileModePatchEntryHeader : PatchEntryHeader
{
    internal NewFileModePatchEntryHeader(TextLine line, int mode)
        : base(line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewFileModeHeader;

    public int Mode { get; }
}
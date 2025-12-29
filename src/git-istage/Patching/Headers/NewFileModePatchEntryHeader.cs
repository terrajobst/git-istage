using GitIStage.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewFileModePatchEntryHeader : PatchEntryHeader
{
    internal NewFileModePatchEntryHeader(Patch root, TextLine line, int mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewFileModeHeader;

    public int Mode { get; }
}
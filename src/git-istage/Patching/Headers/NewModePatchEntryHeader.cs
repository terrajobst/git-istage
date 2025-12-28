using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewModePatchEntryHeader : PatchEntryHeader
{
    internal NewModePatchEntryHeader(Patch root, TextLine line, int mode)
        : base(root, line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewModeHeader;

    public int Mode { get; }
}
using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewModePatchEntryHeader : PatchEntryHeader
{
    internal NewModePatchEntryHeader(TextLine line, int mode)
        : base(line)
    {
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewModeHeader;

    public int Mode { get; }
}
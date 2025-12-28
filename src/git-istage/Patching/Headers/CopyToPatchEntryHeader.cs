using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class CopyToPatchEntryHeader : PatchEntryHeader
{
    internal CopyToPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.CopyToHeader;

    public string Path { get; }
}
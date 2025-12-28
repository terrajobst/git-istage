using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class CopyFromPatchEntryHeader : PatchEntryHeader
{
    internal CopyFromPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.CopyFromHeader;

    public string Path { get; }
}
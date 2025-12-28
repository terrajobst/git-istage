using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class RenameFromPatchEntryHeader : PatchEntryHeader
{
    internal RenameFromPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameFromHeader;

    public string Path { get; }
}
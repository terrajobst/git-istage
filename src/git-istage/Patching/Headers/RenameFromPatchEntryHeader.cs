using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class RenameFromPatchEntryHeader : PatchEntryHeader
{
    internal RenameFromPatchEntryHeader(TextLine line, string path)
        : base(line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameFromHeader;

    public string Path { get; }
}
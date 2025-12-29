using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class RenameToPatchEntryHeader : PatchEntryHeader
{
    internal RenameToPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameToHeader;

    public string Path { get; }
}
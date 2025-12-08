using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class RenameToPatchEntryHeader : PatchEntryHeader
{
    internal RenameToPatchEntryHeader(TextLine line, string path)
        : base(line)
    {
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameToHeader;

    public string Path { get; }
}
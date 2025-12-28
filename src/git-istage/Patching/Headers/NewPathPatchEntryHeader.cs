using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewPathPatchEntryHeader : PatchEntryHeader
{
    internal NewPathPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        ThrowIfNull(path);

        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewPathHeader;

    public string Path { get; }
}
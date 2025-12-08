using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class NewPathPatchEntryHeader : PatchEntryHeader
{
    internal NewPathPatchEntryHeader(TextLine line, string path)
        : base(line)
    {
        ThrowIfNull(path);

        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewPathHeader;

    public string Path { get; }
}
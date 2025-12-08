using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class OldPathPatchEntryHeader : PatchEntryHeader
{
    internal OldPathPatchEntryHeader(TextLine line, string path)
        : base(line)
    {
        ThrowIfNull(path);

        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldPathHeader;

    public string Path { get; }
}
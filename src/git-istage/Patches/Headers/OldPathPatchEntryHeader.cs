using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class OldPathPatchEntryHeader : PatchEntryHeader
{
    internal OldPathPatchEntryHeader(Patch root, TextLine line, string path)
        : base(root, line)
    {
        ThrowIfNull(path);

        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldPathHeader;

    public string Path { get; }
}
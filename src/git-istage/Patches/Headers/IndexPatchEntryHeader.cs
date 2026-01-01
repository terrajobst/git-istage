using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class IndexPatchEntryHeader : PatchEntryHeader
{
    internal IndexPatchEntryHeader(Patch root,
                                   TextLine line,
                                   string hash1,
                                   string hash2,
                                   PatchEntryMode mode)
        : base(root, line)
    {
        Hash1 = hash1;
        Hash2 = hash2;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.IndexHeader;

    public string Hash1 { get; }

    public string Hash2 { get; }
    
    public PatchEntryMode Mode { get; }
}
namespace GitIStage.Patches.EntryHeaders;

public sealed class SimilarityIndexHeader : PatchEntryAdditionalHeader
{
    internal SimilarityIndexHeader(Patch root,
                                   PatchToken similarityKeyword,
                                   PatchToken indexKeyword,
                                   PatchToken<float> percentage)
        : base(root)
    {
        SimilarityKeyword = similarityKeyword;
        IndexKeyword = indexKeyword;
        Percentage = percentage;
    }

    public override PatchNodeKind Kind => PatchNodeKind.SimilarityIndexHeader;

    public PatchToken SimilarityKeyword { get; }

    public PatchToken IndexKeyword { get; }

    public PatchToken<float> Percentage { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        SimilarityKeyword,
        IndexKeyword,
        Percentage
    ];
}
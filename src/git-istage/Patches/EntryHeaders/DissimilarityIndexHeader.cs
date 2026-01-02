namespace GitIStage.Patches.EntryHeaders;

public sealed class DissimilarityIndexHeader : PatchEntryAdditionalHeader
{
    internal DissimilarityIndexHeader(Patch root,
                                      PatchToken dissimilarityKeyword,
                                      PatchToken indexKeyword,
                                      PatchToken<float> percentage)
        : base(root)
    {
        DissimilarityKeyword = dissimilarityKeyword;
        IndexKeyword = indexKeyword;
        Percentage = percentage;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DissimilarityIndexHeader;

    public PatchToken DissimilarityKeyword { get; }

    public PatchToken IndexKeyword { get; }

    public PatchToken<float> Percentage { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        DissimilarityKeyword,
        IndexKeyword,
        Percentage
    ];
}
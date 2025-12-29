using GitIStage.Text;

namespace GitIStage.Patches.Headers;

public sealed class SimilarityIndexPatchEntryHeader : PatchEntryHeader
{
    internal SimilarityIndexPatchEntryHeader(Patch root, TextLine line, int percentage)
        : base(root, line)
    {
        Percentage = percentage;
    }

    public override PatchNodeKind Kind => PatchNodeKind.SimilarityIndexHeader;

    public int Percentage { get; }
}
using GitIStage.Text;

namespace GitIStage.Patching.Headers;

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
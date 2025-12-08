using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class SimilarityIndexPatchEntryHeader : PatchEntryHeader
{
    internal SimilarityIndexPatchEntryHeader(TextLine line, int percentage)
        : base(line)
    {
        Percentage = percentage;
    }

    public override PatchNodeKind Kind => PatchNodeKind.SimilarityIndexHeader;

    public int Percentage { get; }
}
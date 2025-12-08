using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class DissimilarityIndexPatchEntryHeader : PatchEntryHeader
{
    internal DissimilarityIndexPatchEntryHeader(TextLine line, int percentage)
        : base(line)
    {
        Percentage = percentage;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DissimilarityIndexHeader;

    public int Percentage { get; }
}
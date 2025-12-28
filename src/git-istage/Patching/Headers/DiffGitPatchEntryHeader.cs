using GitIStage.Patching.Text;

namespace GitIStage.Patching.Headers;

public sealed class DiffGitPatchEntryHeader : PatchEntryHeader
{
    internal DiffGitPatchEntryHeader(Patch root, TextLine line, string oldPath, string newPath)
        : base(root, line)
    {
        ThrowIfNull(oldPath);
        ThrowIfNull(newPath);

        OldPath = oldPath;
        NewPath = newPath;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DiffGitHeader;

    public string OldPath { get; }

    public string NewPath { get; }
}
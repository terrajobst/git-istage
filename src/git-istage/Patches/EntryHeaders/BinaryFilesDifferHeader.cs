namespace GitIStage.Patches.EntryHeaders;

public sealed class BinaryFilesDifferHeader : PatchEntryAdditionalHeader
{
    internal BinaryFilesDifferHeader(Patch root,
        PatchToken binaryKeyword,
        PatchToken filesKeyword,
        PatchToken<string> oldPath,
        PatchToken andKeyword,
        PatchToken<string> newPath,
        PatchToken differKeyword)
        : base(root)
    {
        BinaryKeyword = binaryKeyword;
        FilesKeyword = filesKeyword;
        OldPath = oldPath;
        AndKeyword = andKeyword;
        NewPath = newPath;
        DifferKeyword = differKeyword;
    }

    public override PatchNodeKind Kind => PatchNodeKind.BinaryFilesDifferHeader;

    public PatchToken BinaryKeyword { get; }

    public PatchToken FilesKeyword { get; }

    public PatchToken<string> OldPath { get; }

    public PatchToken AndKeyword { get; }

    public PatchToken<string> NewPath { get; }

    public PatchToken DifferKeyword { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        BinaryKeyword,
        FilesKeyword,
        OldPath,
        AndKeyword,
        NewPath,
        DifferKeyword
    ];
}
namespace GitIStage.Patches.EntryHeaders;

public sealed class RenameToHeader : PatchEntryAdditionalHeader
{
    internal RenameToHeader(Patch root,
                            PatchToken renameKeyword,
                            PatchToken toKeyword,
                            PatchToken<string> path)
        : base(root)
    {
        RenameKeyword = renameKeyword;
        ToKeyword = toKeyword;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameToHeader;

    public PatchToken RenameKeyword { get; }

    public PatchToken ToKeyword { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        RenameKeyword,
        ToKeyword,
        Path
    ];
}
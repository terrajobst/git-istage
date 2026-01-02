namespace GitIStage.Patches.EntryHeaders;

public sealed class RenameFromHeader : PatchEntryAdditionalHeader
{
    internal RenameFromHeader(Patch root,
                              PatchToken renameKeyword,
                              PatchToken fromKeyword,
                              PatchToken<string> path)
        : base(root)
    {
        RenameKeyword = renameKeyword;
        FromKeyword = fromKeyword;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.RenameFromHeader;

    public PatchToken RenameKeyword { get; }

    public PatchToken FromKeyword { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        RenameKeyword,
        FromKeyword,
        Path
    ];
}
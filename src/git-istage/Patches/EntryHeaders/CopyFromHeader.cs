namespace GitIStage.Patches.EntryHeaders;

public sealed class CopyFromHeader : PatchEntryAdditionalHeader
{
    internal CopyFromHeader(Patch root,
                            PatchToken copyKeyword,
                            PatchToken fromKeyword,
                            PatchToken<string> path)
        : base(root)
    {
        CopyKeyword = copyKeyword;
        FromKeyword = fromKeyword;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.CopyFromHeader;

    public PatchToken CopyKeyword { get; }

    public PatchToken FromKeyword { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        CopyKeyword,
        FromKeyword,
        Path
    ];
}
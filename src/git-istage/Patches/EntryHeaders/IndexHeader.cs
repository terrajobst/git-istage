namespace GitIStage.Patches.EntryHeaders;

public sealed class IndexHeader : PatchEntryAdditionalHeader
{
    internal IndexHeader(Patch root,
                         PatchToken indexKeyword,
                         PatchToken<string> hash1,
                         PatchToken dotDotToken,
                         PatchToken<string> hash2,
                         PatchToken<PatchEntryMode>? mode)
        : base(root)
    {
        IndexKeyword = indexKeyword;
        Hash1 = hash1;
        DotDotToken = dotDotToken;
        Hash2 = hash2;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.IndexHeader;

    public PatchToken IndexKeyword { get; }

    public PatchToken<string> Hash1 { get; }

    public PatchToken DotDotToken { get; }

    public PatchToken<string> Hash2 { get; }

    public PatchToken<PatchEntryMode>? Mode { get; }

    public override IEnumerable<PatchNode> Children()
    {
        yield return IndexKeyword;
        yield return Hash1;
        yield return DotDotToken;
        yield return Hash2;

        if (Mode is not null)
            yield return Mode;
    }
}
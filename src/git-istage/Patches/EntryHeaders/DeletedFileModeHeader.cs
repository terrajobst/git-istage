namespace GitIStage.Patches.EntryHeaders;

public sealed class DeletedFileModeHeader : PatchEntryAdditionalHeader
{
    internal DeletedFileModeHeader(Patch root,
                                   PatchToken deletedKeyword,
                                   PatchToken fileKeyword,
                                   PatchToken modeKeyword,
                                   PatchToken<PatchEntryMode> mode)
        : base(root)
    {
        DeletedKeyword = deletedKeyword;
        FileKeyword = fileKeyword;
        ModeKeyword = modeKeyword;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.DeletedFileModeHeader;

    public PatchToken DeletedKeyword { get; }

    public PatchToken FileKeyword { get; }

    public PatchToken ModeKeyword { get; }

    public PatchToken<PatchEntryMode> Mode { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        DeletedKeyword,
        FileKeyword,
        ModeKeyword,
        Mode
    ];
}
namespace GitIStage.Patches.EntryHeaders;

public sealed class NewFileModeHeader : PatchEntryAdditionalHeader
{
    internal NewFileModeHeader(Patch root,
                               PatchToken newKeyword,
                               PatchToken fileKeyword,
                               PatchToken modeKeyword,
                               PatchToken<PatchEntryMode> mode)
        : base(root)
    {
        NewKeyword = newKeyword;
        FileKeyword = fileKeyword;
        ModeKeyword = modeKeyword;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewFileModeHeader;

    public PatchToken NewKeyword { get; }

    public PatchToken FileKeyword { get; }

    public PatchToken ModeKeyword { get; }

    public PatchToken<PatchEntryMode> Mode { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        NewKeyword,
        FileKeyword,
        ModeKeyword,
        Mode
    ];
}
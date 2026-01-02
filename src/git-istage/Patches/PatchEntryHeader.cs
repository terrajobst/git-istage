namespace GitIStage.Patches;

public sealed class PatchEntryHeader : PatchLine
{
    public PatchEntryHeader(Patch root,
                            PatchToken diffKeyword,
                            PatchToken dashDashToken,
                            PatchToken gitKeyword,
                            PatchToken<string> oldPath,
                            PatchToken<string> newPath)
        : base(root)
    {
        DiffKeyword = diffKeyword;
        DashDashToken = dashDashToken;
        GitKeyword = gitKeyword;
        OldPath = oldPath;
        NewPath = newPath;
    }

    public override PatchNodeKind Kind => PatchNodeKind.EntryHeader;

    public PatchToken DiffKeyword { get; }

    public PatchToken DashDashToken { get; }

    public PatchToken GitKeyword { get; }

    public PatchToken<string> OldPath { get; }

    public PatchToken<string> NewPath { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        DiffKeyword,
        DashDashToken,
        GitKeyword,
        OldPath,
        NewPath
    ];
}
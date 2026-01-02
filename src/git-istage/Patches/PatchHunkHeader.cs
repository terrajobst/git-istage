using GitIStage.Text;

namespace GitIStage.Patches;

public sealed class PatchHunkHeader : PatchLine
{
    internal PatchHunkHeader(Patch root,
                             PatchToken atAt1,
                             PatchToken dashToken,
                             PatchToken<LineRange> oldRange,
                             PatchToken plusToken,
                             PatchToken<LineRange> newRange,
                             PatchToken atAt2,
                             PatchToken<string>? function)
        : base(root)
    {
        ThrowIfNull(oldRange);
        ThrowIfNull(newRange);

        AtAt1 = atAt1;
        DashToken = dashToken;
        OldRange = oldRange;
        PlusToken = plusToken;
        NewRange = newRange;
        AtAt2 = atAt2;
        Function = function;
    }

    public override PatchNodeKind Kind => PatchNodeKind.HunkHeader;

    public PatchToken AtAt1 { get; }

    public PatchToken DashToken { get; }

    public PatchToken<LineRange> OldRange { get; }

    public PatchToken PlusToken { get; }

    public PatchToken<LineRange> NewRange { get; }

    public PatchToken AtAt2 { get; }

    public PatchToken<string>? Function { get; }

    public override IEnumerable<PatchNode> Children()
    {
        yield return AtAt1;
        yield return DashToken;
        yield return OldRange;
        yield return PlusToken;
        yield return NewRange;
        yield return AtAt2;
        if (Function is not null)
            yield return Function;
    }
}
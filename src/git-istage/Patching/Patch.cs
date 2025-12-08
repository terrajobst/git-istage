using System.Collections.Immutable;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

// TODO: Let's replace Patches with this API
//
// PatchDocument  It probably should just hold onto the patch. We probably want to expose some kind of enum on the
//                patch entry to know whether it's a full deletion or not.
//
// FileDocument   Should probably also just hold onto the patch. We should be able to compute the change type from
//                the patch entry (ADD, REMOVED, UPDATED).
//
// These are the change kinds from LibGit2Sharp:
//
//     Unmodified           Not sure if this can happen
//     Added
//     Deleted
//     Modified
//     Renamed
//     Copied
//     Ignored              Not sure if this can happen
//     Untracked            Not sure if this can happen
//     TypeChanged          ? Is this when mode is being changed?
//     Unreadable           ?
//     Conflicted           We should figure out how git diff reports this
//
//     What happens when a file is both copied/renamed and modified?
//
//         [Flags]
//         public enum PatchEntryChanges
//         {
//             None,
//         
//             // Modification
//         
//             Added       = 0b00_0001,
//             Deleted     = 0b00_0010,
//             Modified    = 0b00_0100,
//             ModeChanged = 0b00_1000,
//         
//             // Path
//         
//             Copied      = 0b01_0000,
//             Renamed     = 0b10_0000,
//         }
//
// Or maybe we model this as nullable structs, like so:
//
//        - Each entry has a path. If deleted, that's OldPath, otherwise NewPath
//
//         class PatchEntry
//         
//             Path: string
//         
//             Change { Added, Deleted, Modified }
//         
//             PathChange?
//                 Kind: { Copied, Renamed }
//                 OldPath: string
//                 NewPath: string
//         
//             ModeChange?
//                 OldMode: int
//                 NewMode: int
//
// We should consider handling conflicts, such that we refuse to stage anything from that file, or least the portion
// inside of conflict markers.
//
// In general, Index can't contain conflicts, only working copy can.

public sealed class Patch : PatchNode
{
    public static Patch Empty { get; } = new(SourceText.Empty, []);

    internal Patch(SourceText text, IEnumerable<PatchEntry> entries)
    {
        ThrowIfNull(text);
        ThrowIfNull(entries);

        Text = text;
        Entries = [..entries];
    }

    public override PatchNodeKind Kind => PatchNodeKind.Patch;

    public override TextSpan Span => new TextSpan(0, Text.Length);

    public SourceText Text { get; }

    public ImmutableArray<PatchEntry> Entries { get; }

    public override IEnumerable<PatchNode> Children => Entries;

    public static Patch Parse(string text)
    {
        ThrowIfNull(text);

        if (text.Length == 0)
            return Empty;

        var parser = new PatchParser(text);
        return parser.ParsePatch();
    }

    public override string ToString() => Text.ToString();
}

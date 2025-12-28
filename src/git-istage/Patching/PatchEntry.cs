using System.Collections.Immutable;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

// TODO: We should consider making mode an enum.
//
// From: http://stackoverflow.com/a/8347325/335418:
//
// Mode (Octal)   Meaning
// 040000         Directory
// 100644         Regular non-executable file
// 100664         Regular non-executable group-writeable file
// 100755         Regular executable file
// 120000         Symbolic link
// 160000         Gitlink

public sealed class PatchEntry : PatchNode
{
    internal PatchEntry(IEnumerable<PatchEntryHeader> headers,
                        IEnumerable<PatchHunk> hunks,
                        string oldPath,
                        int oldMode,
                        string newPath,
                        int newMode)
    {
        ThrowIfNull(headers);
        ThrowIfNull(hunks);
        ThrowIfNull(oldPath);
        ThrowIfNull(newPath);

        Headers = [..headers];
        Hunks = [..hunks];
        OldPath = oldPath;
        OldMode = oldMode;
        NewPath = newPath;
        NewMode = newMode;

        var everything = (PatchNode[])[..Headers, ..Hunks];
        var start = everything.Min(n => n.Span.Start);
        var end = everything.Max(n => n.Span.End);
        Span = TextSpan.FromBounds(start, end);
    }

    public override PatchNodeKind Kind => PatchNodeKind.Entry;

    public override TextSpan Span { get; }

    public ImmutableArray<PatchEntryHeader> Headers { get; }

    public ImmutableArray<PatchHunk> Hunks { get; set; }

    public override IEnumerable<PatchNode> Children => [..Headers, ..Hunks];

    public string OldPath { get; }

    public int OldMode { get; }

    public string NewPath { get; }

    public int NewMode { get; }
}
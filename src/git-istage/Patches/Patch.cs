using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using GitIStage.Text;

namespace GitIStage.Patches;

public sealed class Patch : PatchNode
{
    public static Patch Empty { get; } = new(SourceText.Empty);

    private FrozenDictionary<PatchNode, PatchNode?>? _parentByNode;

    internal Patch(SourceText sourceText)
    {
        ThrowIfNull(sourceText);

        var parser = new PatchParser(this, sourceText);

        Text = sourceText;
        Entries = parser.ParseEntries();
        Lines = DescendantsAndSelf().OfType<PatchLine>().ToImmutableArray();

#if DEBUG
        // Ensure no holes between tokens

        var allTokens = DescendantsAndSelf().OfType<PatchToken>();
        var lastEnd = 0;
        foreach (var token in allTokens)
        {
            var hole = TextSpan.FromBounds(lastEnd, token.FullSpan.Start);
            if (hole.Length > 0)
            {
                var position = Text.GetPosition(hole.Start);
                throw new UnreachableException($"detected hole at {position}");
            }

            lastEnd = token.FullSpan.End;
        }

        // Ensure lines cover the entire line

        foreach (var line in Lines)
        {
            if (line.Span != line.TextLine.Span ||
                line.FullSpan != line.TextLine.SpanIncludingLineBreak)
            {
                var lineNumber = line.TextLine.LineIndex + 1;
                throw new UnreachableException($"line {lineNumber} isn't covered");
            }
        }
#endif
    }

    public override PatchNodeKind Kind => PatchNodeKind.Patch;

    public override Patch Root => this;

    public SourceText Text { get; }

    public ImmutableArray<PatchEntry> Entries { get; }

    public ImmutableArray<PatchLine> Lines { get; set; }

    public override IEnumerable<PatchNode> Children() => [..Entries];

    public static Patch Parse(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Empty;

        var sourceText = SourceText.From(text);
        return Parse(sourceText);
    }

    public static Patch Parse(SourceText sourceText)
    {
        if (sourceText == SourceText.Empty)
            return Empty;

        return new Patch(sourceText);
    }

    internal PatchNode? GetParent(PatchNode node)
    {
        if (_parentByNode is null)
        {
            var parentByNode = ComputeParentByNode();
            Interlocked.CompareExchange(ref _parentByNode, parentByNode, null);
        }

        _parentByNode.TryGetValue(node, out var parent);
        return parent;
    }

    private FrozenDictionary<PatchNode, PatchNode?> ComputeParentByNode()
    {
        var parentByNode = new Dictionary<PatchNode, PatchNode?>();
        Walk(this, parentByNode);
        return parentByNode.ToFrozenDictionary();

        static void Walk(PatchNode parent, Dictionary<PatchNode, PatchNode?> parentByNode)
        {
            foreach (var child in parent.Children())
            {
                parentByNode.Add(child, parent);
                Walk(child, parentByNode);
            }
        }
    }

    public override string ToString()
    {
        return Text.ToString();
    }
}
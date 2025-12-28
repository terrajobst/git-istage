using System.Collections.Frozen;
using System.Collections.Immutable;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public sealed class Patch : PatchNode
{
    private FrozenDictionary<PatchNode, PatchNode?>? _parentByNode;

    public static Patch Empty { get; } = new(string.Empty);

    private Patch(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Text = SourceText.Empty;
            Lines = [];
            Entries = [];
        }
        else
        {
            var parser = new PatchParser(this, text);
            var entries = parser.ParseEntries();

            Text = parser.Text;
            Lines = parser.Lines;
            Entries = [.. entries];
        }
    }

    public override PatchNodeKind Kind => PatchNodeKind.Patch;

    public override Patch Root => this;

    public override TextSpan Span => new TextSpan(0, Text.Length);

    public SourceText Text { get; }

    public ImmutableArray<PatchLine> Lines { get; }

    public ImmutableArray<PatchEntry> Entries { get; }

    public override IEnumerable<PatchNode> Children => Entries;

    public static Patch Parse(string text)
    {
        ThrowIfNull(text);

        if (text.Length == 0)
            return Empty;

        return new Patch(text);
    }

    public override string ToString() => Text.ToString();

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
            foreach (var child in parent.Children)
            {
                parentByNode.Add(child, parent);
                Walk(child, parentByNode);
            }
        }
    }
}

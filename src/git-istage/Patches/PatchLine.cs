namespace GitIStage.Patches;

// TODO: Delete this in favor of the new patches
internal sealed class PatchLine
{
    public PatchLine(PatchLineKind kind, string text, string lineBreak)
    {
        Kind = kind;
        Text = text;
        LineBreak = lineBreak;
    }

    public PatchLineKind Kind { get; }

    public string Text { get; }

    public string LineBreak { get; }

    public override string ToString()
    {
        var kindStr = Kind.ToString();
        return $"[{kindStr,-12}] {Text}{LineBreak}";
    }
}
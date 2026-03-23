namespace GitIStage.Text;

public static class PatchClassification
{
    // Line-level
    public static Classification AddedLine { get; } = Classification.Create(nameof(PatchClassification), nameof(AddedLine));
    public static Classification DeletedLine { get; } = Classification.Create(nameof(PatchClassification), nameof(DeletedLine));
    public static Classification AddedLineBackground { get; } = Classification.Create(nameof(PatchClassification), nameof(AddedLineBackground));
    public static Classification DeletedLineBackground { get; } = Classification.Create(nameof(PatchClassification), nameof(DeletedLineBackground));
    public static Classification EntryHeader { get; } = Classification.Create(nameof(PatchClassification), nameof(EntryHeader));

    // Modifiers
    public static Classification AddedLineModifier { get; } = Classification.Create(nameof(PatchClassification), nameof(AddedLineModifier));
    public static Classification DeletedLineModifier { get; } = Classification.Create(nameof(PatchClassification), nameof(DeletedLineModifier));

    // Header tokens
    public static Classification PathToken { get; } = Classification.Create(nameof(PatchClassification), nameof(PathToken));
    public static Classification HashToken { get; } = Classification.Create(nameof(PatchClassification), nameof(HashToken));
    public static Classification ModeToken { get; } = Classification.Create(nameof(PatchClassification), nameof(ModeToken));
    public static Classification TextToken { get; } = Classification.Create(nameof(PatchClassification), nameof(TextToken));
    public static Classification PercentageToken { get; } = Classification.Create(nameof(PatchClassification), nameof(PercentageToken));
    public static Classification RangeToken { get; } = Classification.Create(nameof(PatchClassification), nameof(RangeToken));
    public static Classification MinusMinusMinusToken { get; } = Classification.Create(nameof(PatchClassification), nameof(MinusMinusMinusToken));
    public static Classification PlusPlusPlusToken { get; } = Classification.Create(nameof(PatchClassification), nameof(PlusPlusPlusToken));
    public static Classification Keyword { get; } = Classification.Create(nameof(PatchClassification), nameof(Keyword));
    public static Classification Operator { get; } = Classification.Create(nameof(PatchClassification), nameof(Operator));

    // File list
    public static Classification AddedChange { get; } = Classification.Create(nameof(PatchClassification), nameof(AddedChange));
    public static Classification DeletedChange { get; } = Classification.Create(nameof(PatchClassification), nameof(DeletedChange));
    public static Classification PathText { get; } = Classification.Create(nameof(PatchClassification), nameof(PathText));
}

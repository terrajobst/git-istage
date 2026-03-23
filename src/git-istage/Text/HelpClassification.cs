namespace GitIStage.Text;

public static class HelpClassification
{
    public static Classification CommandKey { get; } = Classification.Create(nameof(HelpClassification), nameof(CommandKey));
    public static Classification Separator { get; } = Classification.Create(nameof(HelpClassification), nameof(Separator));
    public static Classification CommandName { get; } = Classification.Create(nameof(HelpClassification), nameof(CommandName));
    public static Classification CommandDescription { get; } = Classification.Create(nameof(HelpClassification), nameof(CommandDescription));
}

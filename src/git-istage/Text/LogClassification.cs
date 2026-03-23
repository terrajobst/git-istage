namespace GitIStage.Text;

public static class LogClassification
{
    public static Classification Error { get; } = Classification.Create(nameof(LogClassification), nameof(Error));
    public static Classification Info { get; } = Classification.Create(nameof(LogClassification), nameof(Info));
    public static Classification Normal { get; } = Classification.Create(nameof(LogClassification), nameof(Normal));
}

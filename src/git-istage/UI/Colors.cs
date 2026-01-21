using GitIStage.Text;

namespace GitIStage.UI;

internal static class Colors
{
    // Header & Footer

    public static TextColor HeaderForeground => TextColor.Yellow;

    public static TextColor? HeaderBackground => TextColor.DarkGray;

    // Document

    public static TextColor NonExistingTextForeground => TextColor.DarkGray;

    public static TextColor? NonExistingTextBackground => null;

    public static TextColor Selection => TextColor.DarkBlue.Lerp(TextColor.Black, 0.5f).WithAlpha(.3f);

    // Search

    public static TextColor SearchInputForeground => TextColor.Blue;

    public static TextColor? SearchInputBackground => TextColor.Gray;

    // Help

    public static TextColor CommandKeyForeground => TextColor.White;

    public static TextColor CommandNameForeground => TextColor.DarkCyan;

    public static TextColor CommandDescriptionForeground => TextColor.DarkYellow;

    public static TextColor SeparatorForeground => TextColor.DarkGray;

    // Patch

    public static TextColor EntryHeaderForeground => TextColor.White;

    public static TextColor? EntryHeaderBackground => TextColor.DarkCyan;

    public static TextColor PathTokenForeground => TextColor.White;

    public static TextColor HashTokenForeground => TextColor.DarkYellow;

    public static TextColor ModeTokenForeground => TextColor.DarkYellow;

    public static TextColor TextTokenForeground => TextColor.Gray;

    public static TextColor PercentageTokenForeground => TextColor.DarkMagenta;

    public static TextColor RangeTokenForeground => TextColor.Cyan;

    public static TextColor MinusMinusMinusTokenForeground => TextColor.DarkRed;

    public static TextColor PlusPlusPlusTokenForeground => TextColor.Green;

    public static TextColor KeywordForeground => TextColor.Cyan;

    public static TextColor OperatorForeground => TextColor.DarkCyan;

    public static TextColor AddedText => TextColor.DarkGreen.Lerp(TextColor.Black, .15f);

    public static TextColor DeletedText => TextColor.DarkRed.Lerp(TextColor.Black, .15f);

    public static TextColor PathText => TextColor.DarkCyan;
}
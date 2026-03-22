using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal static class Colors
{
    private static SyntaxTheme Theme => SyntaxTheme.Instance;

    // Header & Footer

    public static TextColor HeaderForeground => Theme.GetGuiColor("statusBarItem.remoteForeground") ?? TextColor.White;

    public static TextColor? HeaderBackground => Theme.GetGuiColor("statusBarItem.remoteBackground");

    // Document

    public static TextColor NonExistingTextForeground => Theme.GetGuiColor("input.placeholderForeground") ?? TextColor.DarkGray;

    public static TextColor? NonExistingTextBackground => null;

    public static TextColor Selection => Theme.GetGuiColor("editor.inactiveSelectionBackground") ?? TextColor.DarkBlue.Lerp(TextColor.Black, 0.5f).WithAlpha(.8f);

    // Search

    public static TextColor SearchInputForeground => Theme.GetGuiColor("editor.foreground") ?? TextColor.White;

    public static TextColor? SearchInputBackground => Theme.GetGuiColor("editor.background");

    // Help

    public static TextColor CommandKeyForeground => Theme.GetScopeColor("entity.name.function");

    public static TextColor CommandNameForeground => Theme.GetScopeColor("support.function");

    public static TextColor CommandDescriptionForeground => Theme.GetScopeColor("string");

    public static TextColor SeparatorForeground => Theme.GetScopeColor("comment");

    // Patch

    public static TextColor EntryHeaderForeground => Theme.GetScopeColor("meta.diff.header");

    public static TextColor PathTokenForeground => Theme.GetScopeColor("string");

    public static TextColor HashTokenForeground => Theme.GetScopeColor("constant.numeric");

    public static TextColor ModeTokenForeground => Theme.GetScopeColor("constant.numeric");

    public static TextColor TextTokenForeground => Theme.GetGuiColor("editor.foreground") ?? TextColor.Gray;

    public static TextColor PercentageTokenForeground => Theme.GetScopeColor("constant.numeric");

    public static TextColor RangeTokenForeground => Theme.GetScopeColor("keyword.control");

    public static TextColor MinusMinusMinusTokenForeground => Theme.GetScopeColor("markup.deleted");

    public static TextColor PlusPlusPlusTokenForeground => Theme.GetScopeColor("markup.inserted");

    public static TextColor KeywordForeground => Theme.GetScopeColor("keyword.other.diff");

    public static TextColor OperatorForeground => Theme.GetScopeColor("keyword.other.diff");

    public static TextColor AddedText => Theme.GetScopeColor("markup.inserted");

    public static TextColor DeletedText => Theme.GetScopeColor("markup.deleted");

    public static TextColor PathText => Theme.GetScopeColor("entity.name.type");

    // Log

    public static TextColor LogErrorForeground => Theme.GetScopeColor("markup.deleted");

    public static TextColor LogInfoForeground => Theme.GetScopeColor("comment");

    public static TextColor LogNormalForeground => Theme.GetGuiColor("editor.foreground") ?? TextColor.White;
}

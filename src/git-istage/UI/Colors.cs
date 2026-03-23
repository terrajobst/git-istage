using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class Colors
{
    private readonly ThemeService _theme;

    public Colors(ThemeService theme)
    {
        _theme = theme;
    }

    // Header & Footer

    public TextColor HeaderForeground => _theme.GetGuiColor("statusBarItem.remoteForeground") ?? TextColor.White;

    public TextColor? HeaderBackground => _theme.GetGuiColor("statusBarItem.remoteBackground");

    // Document

    public TextColor NonExistingTextForeground => _theme.GetGuiColor("input.placeholderForeground") ?? TextColor.DarkGray;

    public TextColor? NonExistingTextBackground => null;

    public TextColor Selection => _theme.GetGuiColor("editor.inactiveSelectionBackground") ?? TextColor.DarkBlue.Lerp(TextColor.Black, 0.5f).WithAlpha(.8f);

    // Search

    public TextColor SearchInputForeground => _theme.GetGuiColor("editor.foreground") ?? TextColor.White;

    public TextColor? SearchInputBackground => _theme.GetGuiColor("editor.background");
}

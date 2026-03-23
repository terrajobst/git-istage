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

    public TextColor HeaderForeground => _theme.GetGuiColor("statusBarItem.remoteForeground") ?? DefaultForeground ?? TextColor.White;

    public TextColor? HeaderBackground => _theme.GetGuiColor("statusBarItem.remoteBackground");

    // Default

    public TextColor? DefaultForeground => _theme.GetGuiColor("editor.foreground");

    public TextColor? DefaultBackground => _theme.GetGuiColor("editor.background");

    // Document

    public TextColor NonExistingTextForeground => _theme.GetGuiColor("input.placeholderForeground") ?? TextColor.DarkGray;

    public TextColor? NonExistingTextBackground => DefaultBackground;

    public TextColor Selection => _theme.GetGuiColor("editor.selectionBackground")
                             ?? TextColor.Blue;

    // Search

    public TextColor SearchInputForeground => DefaultForeground ?? TextColor.White;

    public TextColor? SearchInputBackground => DefaultBackground;
}

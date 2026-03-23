using System.Runtime.InteropServices;
using GitIStage.Text;
using GitIStage.UI;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace GitIStage.Services;

internal sealed class ThemeService
{
    private Registry _registry;
    private Theme _theme;
    private Dictionary<Classification, TextStyle> _classificationStyles;
    private Dictionary<Classification, TextStyle> _syntaxStyleCache = new();

    public ThemeService()
    {
        _registry = new Registry(new RegistryOptions(ThemeName.DarkPlus));
        _theme = _registry.GetTheme();
        _classificationStyles = CreateClassificationStyles();
        Colors = new Colors(this);
    }

    public Colors Colors { get; }

    public Theme Theme => _theme;

    public void SetTheme(ThemeName themeName)
    {
        _registry = new Registry(new RegistryOptions(themeName));
        _theme = _registry.GetTheme();
        _classificationStyles = CreateClassificationStyles();
        _syntaxStyleCache = new();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsKnownClassification(Classification classification)
    {
        return _classificationStyles.ContainsKey(classification);
    }

    public TextStyle ResolveStyle(Classification classification)
    {
        if (_classificationStyles.TryGetValue(classification, out var style))
            return style;

        if (!_syntaxStyleCache.TryGetValue(classification, out style))
        {
            style = ResolveSyntaxStyle(classification);
            _syntaxStyleCache.Add(classification, style);
        }

        return style;
    }

    public TextColor? GetGuiColor(string key)
    {
        var guiColors = _theme.GetGuiColorDictionary();
        return guiColors.TryGetValue(key, out var hex) ? TextColor.FromHex(hex) : null;
    }

    private Dictionary<Classification, TextStyle> CreateClassificationStyles()
    {
        var styles = new Dictionary<Classification, TextStyle>();

        var addedText = GetScopeColor("markup.inserted");
        var deletedText = GetScopeColor("markup.deleted");

        // Patch - line-level
        AddLineStyle(styles, PatchClassification.AddedLine, addedText);
        AddLineStyle(styles, PatchClassification.DeletedLine, deletedText);
        AddBackgroundStyle(styles, PatchClassification.AddedLineBackground, addedText);
        AddBackgroundStyle(styles, PatchClassification.DeletedLineBackground, deletedText);
        AddLineStyle(styles, PatchClassification.EntryHeader, GetScopeColor("meta.diff.header"));

        // Patch - modifiers
        AddForegroundStyle(styles, PatchClassification.AddedLineModifier, addedText);
        AddForegroundStyle(styles, PatchClassification.DeletedLineModifier, deletedText);

        // Patch - header tokens
        AddForegroundStyle(styles, PatchClassification.PathToken, GetScopeColor("string"));
        AddForegroundStyle(styles, PatchClassification.HashToken, GetScopeColor("constant.numeric"));
        AddForegroundStyle(styles, PatchClassification.ModeToken, GetScopeColor("constant.numeric"));
        AddForegroundStyle(styles, PatchClassification.TextToken, GetGuiColor("editor.foreground") ?? TextColor.White);
        AddForegroundStyle(styles, PatchClassification.PercentageToken, GetScopeColor("constant.numeric"));
        AddForegroundStyle(styles, PatchClassification.RangeToken, GetScopeColor("keyword.control"));
        AddForegroundStyle(styles, PatchClassification.MinusMinusMinusToken, deletedText);
        AddForegroundStyle(styles, PatchClassification.PlusPlusPlusToken, addedText);
        AddForegroundStyle(styles, PatchClassification.Keyword, GetScopeColor("keyword.other.diff"));
        AddForegroundStyle(styles, PatchClassification.Operator, GetScopeColor("keyword.other.diff"));

        // Patch - file list
        AddForegroundStyle(styles, PatchClassification.AddedChange, addedText);
        AddForegroundStyle(styles, PatchClassification.DeletedChange, deletedText);
        AddForegroundStyle(styles, PatchClassification.PathText, GetScopeColor("entity.name.type"));

        // Help
        AddForegroundStyle(styles, HelpClassification.CommandKey, GetScopeColor("entity.name.function"));
        AddForegroundStyle(styles, HelpClassification.Separator, GetScopeColor("comment"));
        AddForegroundStyle(styles, HelpClassification.CommandName, GetScopeColor("support.function"));
        AddForegroundStyle(styles, HelpClassification.CommandDescription, GetScopeColor("string"));

        // Log
        AddForegroundStyle(styles, LogClassification.Error, deletedText);
        AddForegroundStyle(styles, LogClassification.Info, GetScopeColor("comment"));
        AddForegroundStyle(styles, LogClassification.Normal, GetGuiColor("editor.foreground") ?? TextColor.White);

        return styles;
    }

    private static void AddForegroundStyle(Dictionary<Classification, TextStyle> styles, Classification classification, TextColor foreground)
    {
        styles.Add(classification, new TextStyle { Foreground = foreground });
    }

    private static void AddLineStyle(Dictionary<Classification, TextStyle> styles, Classification classification, TextColor foreground)
    {
        styles.Add(classification, new TextStyle
        {
            Foreground = foreground,
            Background = foreground.Lerp(TextColor.Black, 0.8f)
        });
    }

    private static void AddBackgroundStyle(Dictionary<Classification, TextStyle> styles, Classification classification, TextColor foreground)
    {
        styles.Add(classification, new TextStyle
        {
            Background = foreground.Lerp(TextColor.Black, 0.8f)
        });
    }

    private TextColor GetScopeColor(params string[] scopes)
    {
        foreach (var rule in _theme.Match(scopes))
        {
            if (rule.foreground > 0)
                return TextColor.FromHex(_theme.GetColor(rule.foreground));
        }

        return GetGuiColor("editor.foreground") ?? TextColor.White;
    }

    private TextStyle ResolveSyntaxStyle(Classification classification)
    {
        var scopes = ImmutableCollectionsMarshal.AsArray(classification.Scopes)!;
        if (scopes.Length == 0)
            return default;

        var foreground = -1;
        var background = -1;
        var fontStyle = FontStyle.None;

        foreach (var rule in _theme.Match(scopes))
        {
            if (foreground == -1 && rule.foreground > 0)
                foreground = rule.foreground;

            if (background == -1 && rule.background > 0)
                background = rule.background;

            if (fontStyle == FontStyle.None && rule.fontStyle != FontStyle.None)
                fontStyle = rule.fontStyle;
        }

        return new TextStyle
        {
            Foreground = foreground > 0 ? TextColor.FromHex(_theme.GetColor(foreground)) : null,
            Background = background > 0 ? TextColor.FromHex(_theme.GetColor(background)) : null,
            Attributes = GetAttributes(fontStyle)
        };
    }

    private static TextAttributes GetAttributes(FontStyle fontStyle)
    {
        var result = TextAttributes.None;

        if (fontStyle == FontStyle.NotSet)
            return result;

        if ((fontStyle & FontStyle.Italic) != 0)
            result |= TextAttributes.Italic;

        if ((fontStyle & FontStyle.Bold) != 0)
            result |= TextAttributes.Bold;

        if ((fontStyle & FontStyle.Underline) != 0)
            result |= TextAttributes.Underline;

        if ((fontStyle & FontStyle.Strikethrough) != 0)
            result |= TextAttributes.Strike;

        return result;
    }

    public event EventHandler? ThemeChanged;
}

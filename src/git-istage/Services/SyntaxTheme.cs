using System.Collections.Concurrent;
using GitIStage.Text;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using IGrammar = TextMateSharp.Grammars.IGrammar;

namespace GitIStage.Services;

internal sealed class SyntaxTheme
{
    public static SyntaxTheme Instance { get; } = new();

    private readonly RegistryOptions _options;
    private readonly Registry _registry;
    private readonly Theme _theme;
    private readonly ConcurrentDictionary<string, IGrammar?> _grammarByExtensions = new();

    private SyntaxTheme()
    {
        _options = new RegistryOptions(ThemeName.DarkPlus);
        _registry = new Registry(_options);
        _theme = _registry.GetTheme();
    }

    public Theme Theme => _theme;

    public TextColor GetScopeColor(params string[] scopes)
    {
        foreach (var rule in _theme.Match(scopes))
        {
            if (rule.foreground > 0)
                return TextColor.FromHex(_theme.GetColor(rule.foreground));
        }

        return GetGuiColor("editor.foreground") ?? TextColor.White;
    }

    public TextColor? GetGuiColor(string key)
    {
        var guiColors = _theme.GetGuiColorDictionary();
        return guiColors.TryGetValue(key, out var hex) ? TextColor.FromHex(hex) : null;
    }

    public IGrammar? GetGrammar(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return _grammarByExtensions.GetOrAdd(extension, static (ext, theme) =>
        {
            lock (theme._registry)
            {
                var initialScopeName = theme._options.GetScopeByExtension(ext);
                return theme._registry.LoadGrammar(initialScopeName);
            }
        }, this);
    }
}

using System.Collections.Concurrent;
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

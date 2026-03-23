using System.Collections.Concurrent;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using IGrammar = TextMateSharp.Grammars.IGrammar;

namespace GitIStage.Services;

internal sealed class SyntaxTokenizer
{
    public static SyntaxTokenizer Instance { get; } = new();

    private readonly RegistryOptions _options;
    private readonly Registry _registry;
    private readonly ConcurrentDictionary<string, IGrammar?> _grammarByExtensions = new();

    private SyntaxTokenizer()
    {
        _options = new RegistryOptions(ThemeName.DarkPlus);
        _registry = new Registry(_options);
    }

    public IGrammar? GetGrammar(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return _grammarByExtensions.GetOrAdd(extension, static (ext, tokenizer) =>
        {
            lock (tokenizer._registry)
            {
                var initialScopeName = tokenizer._options.GetScopeByExtension(ext);
                return tokenizer._registry.LoadGrammar(initialScopeName);
            }
        }, this);
    }
}

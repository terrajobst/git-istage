using System.Text.Json;
using System.Text.Json.Serialization;
using GitIStage.Commands;

namespace GitIStage.Services;

internal sealed class KeyBindingService
{
    public IReadOnlyDictionary<string, IReadOnlyList<ConsoleKeyBinding>> GetUserKeyBindings()
    {
        var result = new Dictionary<string, IReadOnlyList<ConsoleKeyBinding>>();
        var userKeyBindings = LoadUserKeyBindings();
        if (userKeyBindings is null)
            return result;
       
        foreach (var (name, bindingData) in userKeyBindings)
        {
            if (bindingData?.KeyBindings is not { Length: > 0 })
                continue;

            var bindings = new List<ConsoleKeyBinding>();

            if (bindingData.KeyBindings is not null)
            {
                foreach (var bindingText in bindingData.KeyBindings)
                {
                    if (bindingText is null)
                        continue;

                    if (!ConsoleKeyBinding.TryParse(bindingText, out var binding))
                    {
                        Console.WriteLine($"fatal: invalid key binding for '{name}': {binding}");
                        Environment.Exit(1);
                    }

                    bindings.Add(binding);
                }
            }

            result.Add(name, bindings.ToArray());
        }

        return result;
    }

    private static string UserKeyBindingsPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Join(homeDirectory, ".git-istage", "key-bindings.json");
    }

    private static IDictionary<string, CustomKeyBinding?>? LoadUserKeyBindings()
    {
        var path = UserKeyBindingsPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        
        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, CustomKeyBinding?>>(content);
    }

    private sealed class CustomKeyBinding
    {
        // set to an empty array (or null) to clear the default bindings
        // for the corresponding command

        [JsonPropertyName("default")]
        public string?[]? Default { get; set; }

        [JsonPropertyName("keyBindings")]
        public string?[]? KeyBindings { get; set; }
    }
}
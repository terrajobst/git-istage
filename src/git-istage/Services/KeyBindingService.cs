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
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, CustomKeyBinding?>>(content, options);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"fatal: user key bindings in {path} are malformed: {ex.Message}");
            Environment.Exit(1);
            throw;
        }
    }

    private sealed class CustomKeyBinding
    {
        [JsonPropertyName("keyBindings")]
        public string?[]? KeyBindings { get; set; }
    }
}
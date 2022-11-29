using System.Text.Json;
using System.Text.Json.Serialization;
using GitIStage.Commands;

namespace GitIStage.Services;

internal sealed class KeyBindingService
{
    private readonly UserEnvironment _userEnvironment;

    public KeyBindingService(UserEnvironment userEnvironment)
    {
        _userEnvironment = userEnvironment;
    }

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
                        throw ExceptionBuilder.KeyBindingInvalidBinding(GetUserKeyBindingsPath(), name, bindingText);

                    bindings.Add(binding);
                }
            }

            if (bindings.Any())
                result.Add(name, bindings.ToArray());
        }

        return result;
    }

    public string GetUserKeyBindingsPath()
    {
        var homeDirectory = _userEnvironment.SettingsDirectory;
        return Path.Join(homeDirectory, "key-bindings.json");
    }

    private IDictionary<string, CustomKeyBinding?>? LoadUserKeyBindings()
    {
        var path = GetUserKeyBindingsPath();
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
            throw ExceptionBuilder.KeyBindingsAreInvalidJson(GetUserKeyBindingsPath(), ex);
        }
    }

    private sealed class CustomKeyBinding
    {
        [JsonPropertyName("keyBindings")]
        public string?[]? KeyBindings { get; set; }
    }
}
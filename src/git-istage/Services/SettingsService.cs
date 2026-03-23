using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GitIStage.Commands;
using TextMateSharp.Grammars;

namespace GitIStage.Services;

internal sealed class SettingsService
{
    private readonly UserEnvironment _userEnvironment;

    public SettingsService(UserEnvironment userEnvironment)
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

    public ThemeName GetTheme()
    {
        var settings = LoadSettings();

        if (settings is not null &&
            settings.TryGetPropertyValue("theme", out var themeNode) &&
            themeNode is not null &&
            Enum.TryParse<ThemeName>(themeNode.GetValue<string>(), ignoreCase: true, out var themeName))
        {
            return themeName;
        }

        return ThemeName.DarkPlus;
    }

    public void SaveTheme(ThemeName themeName)
    {
        SaveSetting("theme", themeName.ToString());
    }

    public bool GetSyntaxHighlighting()
    {
        var settings = LoadSettings();

        if (settings is not null &&
            settings.TryGetPropertyValue("syntaxHighlighting", out var node) &&
            node is not null)
        {
            return node.GetValue<bool>();
        }

        return true;
    }

    public void SaveSyntaxHighlighting(bool enabled)
    {
        SaveSetting("syntaxHighlighting", enabled);
    }

    private void SaveSetting(string key, JsonNode value)
    {
        var settings = LoadSettings() ?? new JsonObject();
        settings[key] = value;

        var settingsPath = GetSettingsPath();
        var directory = Path.GetDirectoryName(settingsPath)!;
        Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(settingsPath, settings.ToJsonString(options));
    }

    public string GetUserKeyBindingsPath()
    {
        var homeDirectory = _userEnvironment.SettingsDirectory;
        return Path.Join(homeDirectory, "key-bindings.json");
    }

    private string GetSettingsPath()
    {
        return Path.Join(_userEnvironment.SettingsDirectory, "settings.json");
    }

    private JsonObject? LoadSettings()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return null;

        try
        {
            var content = File.ReadAllText(path);
            return JsonNode.Parse(content) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
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
            // The built-in dictionary deserialization doesn't like it when it finds a $schema key.
            // Since the schema is only useful for editors, we just remove it before attempting to
            // deserialize.

            if (JsonNode.Parse(content) is JsonObject jsonObject)
            {
                if (jsonObject.Remove("$schema"))
                    content = jsonObject.ToJsonString();
            }

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

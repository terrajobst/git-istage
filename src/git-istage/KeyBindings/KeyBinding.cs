using System.Text.Json.Serialization;

namespace GitIStage;

public class KeyBinding
{
    [JsonPropertyName("default")]
    public List<string> Default { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
}
public class CustomKeyBinding
{
    // set to an empty array (or null) to clear the default bindings
    // for the corresponding command

    [JsonPropertyName("default")]
    public string[] Default { get; set; }

    [JsonPropertyName("keyBindings")]
    public string[] KeyBindings { get; set; }
}
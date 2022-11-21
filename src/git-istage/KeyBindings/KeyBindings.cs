using System.Text.Json.Serialization;

namespace GitIStage
{
    public partial class KeyBindings
    {
        [JsonIgnore]
        public IDictionary<string, KeyBinding> Handlers { get; set; }
    }
}
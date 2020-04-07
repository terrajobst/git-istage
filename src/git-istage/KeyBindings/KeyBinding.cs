using Newtonsoft.Json;

namespace GitIStage
{
    public class KeyBinding
    {
        [JsonProperty("default")]
        public string[] Default { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }
    public class CustomKeyBinding
    {
        [JsonProperty("before")]
        public string Before { get; set; }
        [JsonProperty("after")]
        public string After { get; set; }
    }

}
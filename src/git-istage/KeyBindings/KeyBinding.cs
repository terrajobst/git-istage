using Newtonsoft.Json;
using System.Collections.Generic;

namespace GitIStage
{
    public class KeyBinding
    {
        [JsonProperty("default")]
        public List<string> Default { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }
    public class CustomKeyBinding
    {
        // set to an empty array (or null) to clear the default bindings
        // for the corresponding command

        [JsonProperty("default")]
        public string[] Default { get; set; }

        [JsonProperty("keyBindings")]
        public string[] KeyBindings { get; set; }
    }

}
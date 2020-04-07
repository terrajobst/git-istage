using Newtonsoft.Json;
using System.Collections.Generic;

namespace GitIStage
{
    public class KeyBindings
    {
        [JsonIgnore()]
        public IDictionary<string, KeyBinding> Handlers { get; set; }
    }
}
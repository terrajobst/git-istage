using Newtonsoft.Json;
using System.Collections.Generic;

namespace GitIStage
{
    public partial class KeyBindings
    {
        [JsonIgnore()]
        public IDictionary<string, KeyBinding> Handlers { get; set; }
    }
}
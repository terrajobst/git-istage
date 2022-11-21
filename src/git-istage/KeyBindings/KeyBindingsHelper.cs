using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GitIStage
{
    public partial class KeyBindings
    {
        public static KeyBindings Load()
        {
            // load default key bindings from resource

            KeyBindings keyBindings = null;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("git-istage.key-bindings.json"))
                keyBindings = LoadKeyBindings(stream);

            // merge custom key bindings

            var keyBindingsPath = ResolveCustomKeyBindingsPath();
            if (!string.IsNullOrEmpty(keyBindingsPath) && File.Exists(keyBindingsPath))
            {
                var customKeyBindings = LoadCustomKeyBindings(keyBindingsPath);
                foreach (var customKeyBinding in customKeyBindings)
                {
                    var command = customKeyBinding.Key;
                    var key = customKeyBinding.Value.KeyBindings?.FirstOrDefault();
                    if (key != null)
                    {
                        var binding = keyBindings.Handlers.SingleOrDefault(h => string.Equals(h.Key, command, StringComparison.OrdinalIgnoreCase)).Value;
                        if (binding != null)
                        {
                            if (customKeyBinding.Value.Default?.Length == 0)
                                binding.Default.Clear();
                            binding.Default.Add(key);
                        }
                    }
                }
            }

            return keyBindings;
        }

        private static IDictionary<string, CustomKeyBinding> LoadCustomKeyBindings(string keyBindingsPath)
        {
            var content = File.ReadAllText(keyBindingsPath);
            return JsonSerializer.Deserialize<Dictionary<string, CustomKeyBinding>>(content);
        }

        private static KeyBindings LoadKeyBindings(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var keyBindings = new KeyBindings();
            keyBindings.Handlers = JsonSerializer.Deserialize<Dictionary<string, KeyBinding>>(content);
            return keyBindings;
        }

        private static string ResolveCustomKeyBindingsPath()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDirectory, ".git-istage/key-bindings.json");
        }
    }
}
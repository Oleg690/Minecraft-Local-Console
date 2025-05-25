using System.IO;
using System.Text.Json;

namespace Minecraft_Console
{
    class JsonHelper
    {
        private static readonly JsonSerializerOptions CachedJsonOptions = new() { WriteIndented = true };

        public static void CreateJsonIfNotExists(string filePath, Dictionary<string, object> defaultValues)
        {
            if (!File.Exists(filePath))
            {
                string json = JsonSerializer.Serialize(defaultValues, CachedJsonOptions);
                File.WriteAllText(filePath, json);
                Console.WriteLine("Created config file.");
            }
        }

        public static object? GetOrSetValue(string filePath, string key, object? newValue = null)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Config file not found.");
                return null;
            }

            string json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? [];

            if (newValue == null)
            {
                // Getter
                return dict.TryGetValue(key, out object? value) ? value : null;
            }
            else
            {
                // Setter
                dict[key] = newValue;
                string updatedJson = JsonSerializer.Serialize(dict, CachedJsonOptions);
                File.WriteAllText(filePath, updatedJson);
                return newValue;
            }
        }
    }
}

using Jither.Imuse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImuseSequencer
{
    public class Settings
    {
        private const string settingsPath = "settings.json";

        public static readonly Settings Default = Load();

        public string MidiFolder { get; set; } = ".";
        public Dictionary<SoundTarget, string> Devices { get; set; } = new();

        public Settings()
        {
        }

        private static Settings Load()
        {
            if (!File.Exists(settingsPath))
            {
                return new Settings();
            }
            string json = File.ReadAllText(settingsPath);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Deserialize<Settings>(json, jsonOptions);
        }
    }
}

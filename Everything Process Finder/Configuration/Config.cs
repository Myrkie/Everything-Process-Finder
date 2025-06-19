using System.Text.Json;
using System.Text.Json.Serialization;

namespace Everything_Process_Finder.Configuration
{
    [JsonSerializable(typeof(Config))]
    [JsonSerializable(typeof(HighlighterConfig))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true, AllowTrailingCommas = true)]
    internal partial class ConfigSourceGenerationContext : JsonSerializerContext;
    
    [Serializable]
    public class HighlighterConfig
    {
        public bool DrawHighlighter { get; set; } = true;
        
        [JsonConverter(typeof(SafeColorConverter))]
        public Color HighlightColor { get; set; } = Color.Purple;
    }

    [Serializable]
    public class Config
    {
        static readonly string ConfigPath = $"{AppContext.BaseDirectory}config.json";
        public static Config Instance { get; } = LoadConfig();
        public bool RunOnStartup { get; set; } = true;
        public int WindowCheckLoopInterval { get; set; } = 1;
        public HighlighterConfig Highlighter { get; set; } = new();

        static Config LoadConfig()
        {
            Config? cfg = File.Exists(ConfigPath) ? JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), ConfigSourceGenerationContext.Default.Config) : null;
            if (cfg != null) return cfg;
            cfg = new Config();
            cfg.SaveConfig();

            return cfg;
        }

        public void SaveConfig()
        {
            string json = JsonSerializer.Serialize(this, ConfigSourceGenerationContext.Default.Config);
            File.WriteAllText(ConfigPath, json);
        }
    }
}

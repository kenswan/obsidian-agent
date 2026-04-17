using System.Text.Json;

namespace ObsidianAgent.Mcp.Configuration;

public class UserSettings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".obsidian-agent");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? VaultName { get; set; }

    public static UserSettings Load()
    {
        if (!File.Exists(SettingsFile))
        {
            return new UserSettings();
        }

        try
        {
            string json = File.ReadAllText(SettingsFile);
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(SettingsFile, json);
    }
}

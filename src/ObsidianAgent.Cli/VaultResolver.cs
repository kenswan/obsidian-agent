using System.Text.RegularExpressions;

namespace ObsidianAgent.Cli;

public static class VaultResolver
{
    private const string DefaultVault = "sample-vault";

    public static string Resolve(string mcpProjectPath)
    {
        // Check user settings first (~/.obsidian-agent/settings.json)
        try
        {
            string userSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".obsidian-agent", "settings.json");

            if (File.Exists(userSettingsPath))
            {
                string json = File.ReadAllText(userSettingsPath);
                Match match = Regex.Match(json, "\"vaultName\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }

        // Fall back to MCP project appsettings.json
        try
        {
            string appSettingsPath = Path.Combine(mcpProjectPath, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                string json = File.ReadAllText(appSettingsPath);
                Match match = Regex.Match(json, "\"VaultName\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }

        return DefaultVault;
    }
}

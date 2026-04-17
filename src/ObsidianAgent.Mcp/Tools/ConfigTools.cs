using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class ConfigTools(IOptions<ObsidianOptions> options)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("Get the current vault name, or set a new vault name that persists across sessions")]
    public object SetVault(string? vaultName = null)
    {
        if (vaultName is null)
        {
            return new { config.VaultName };
        }

        var settings = UserSettings.Load();
        settings.VaultName = vaultName;
        settings.Save();

        return new { VaultName = vaultName, Persisted = true, Note = "Restart the server for this change to take effect." };
    }
}

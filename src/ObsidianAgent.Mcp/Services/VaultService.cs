using Microsoft.Extensions.Options;
using ObsidianAgent.Mcp.Configuration;

namespace ObsidianAgent.Mcp.Services;

public class VaultService(IOptions<ObsidianOptions> options, ILogger<VaultService> logger)
{
    private readonly ObsidianOptions config = options.Value;

    public string GetVaultPath()
    {
        string path = Path.GetFullPath(config.VaultPath);

        if (!Directory.Exists(path))
        {
            logger.LogWarning("Vault path does not exist: {VaultPath}", path);
        }

        return path;
    }

    public string ResolveNotePath(string notePath)
    {
        string vaultPath = GetVaultPath();

        string fullPath = notePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(vaultPath, notePath)
            : Path.Combine(vaultPath, notePath + ".md");

        return Path.GetFullPath(fullPath);
    }

    public bool IsInsideVault(string filePath)
    {
        string fullFilePath = Path.GetFullPath(filePath);
        string fullVaultPath = Path.GetFullPath(GetVaultPath());
        return fullFilePath.StartsWith(fullVaultPath, StringComparison.OrdinalIgnoreCase);
    }
}

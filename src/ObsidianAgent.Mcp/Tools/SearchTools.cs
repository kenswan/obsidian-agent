using System.ComponentModel;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class SearchTools(
    VaultService vaultService,
    ILogger<SearchTools> logger)
{
    [McpServerTool, Description("Search for notes containing the given text query")]
    public async Task<object> SearchNotes(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching notes for '{Query}'", query);

        string vaultPath = vaultService.GetVaultPath();

        if (!Directory.Exists(vaultPath))
        {
            return new { Query = query, Count = 0, Matches = Array.Empty<object>(), Error = "Vault path not found" };
        }

        List<object> matches = [];

        foreach (string filePath in Directory.EnumerateFiles(vaultPath, "*.md", SearchOption.AllDirectories))
        {
            if (matches.Count >= limit)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            string content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = Path.GetRelativePath(vaultPath, filePath);
                matches.Add(new { Path = relativePath });
            }
        }

        return new { Query = query, Count = matches.Count, Matches = matches };
    }
}

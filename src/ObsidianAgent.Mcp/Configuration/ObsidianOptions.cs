namespace ObsidianAgent.Mcp.Configuration;

public class ObsidianOptions
{
    public const string SectionName = "Obsidian";

    public string VaultName { get; set; } = "sample-vault";
    public string CliCommand { get; set; } = "obsidian";
    public string DailyNoteFormat { get; set; } = "yyyy-MM-dd";
    public string DailyNoteFolder { get; set; } = "Daily";
    public string TemplateFolder { get; set; } = "Templates";
    public bool Verbose { get; set; }
}

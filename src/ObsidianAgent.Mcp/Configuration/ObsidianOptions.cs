namespace ObsidianAgent.Mcp.Configuration;

public class ObsidianOptions
{
    public const string SectionName = "Obsidian";

    public string VaultName { get; set; } = "Default";
    public string VaultPath { get; set; } = "";
    public string DailyNoteFormat { get; set; } = "yyyy-MM-dd";
    public string DailyNoteFolder { get; set; } = "Daily";
    public string TemplateFolder { get; set; } = "Templates";
}

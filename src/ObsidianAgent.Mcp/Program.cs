using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool verbose = args.Contains("--verbose");

builder.Services.AddSingleton<ICliRunner, CliRunner>();
builder.Services.AddSingleton<IObsidianCliService, ObsidianCliService>();

builder.Services
    .AddOptions<ObsidianOptions>()
    .BindConfiguration(ObsidianOptions.SectionName);

builder.Services.PostConfigure<ObsidianOptions>(opts =>
{
    if (verbose) opts.Verbose = true;

    var userSettings = UserSettings.Load();
    if (!string.IsNullOrEmpty(userSettings.VaultName))
    {
        opts.VaultName = userSettings.VaultName;
    }
});

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

await app.RunAsync().ConfigureAwait(false);

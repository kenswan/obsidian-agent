using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICliRunner, CliRunner>();
builder.Services.AddSingleton<VaultService>();

builder.Services
    .AddOptions<ObsidianOptions>()
    .BindConfiguration(ObsidianOptions.SectionName);

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

await app.RunAsync().ConfigureAwait(false);

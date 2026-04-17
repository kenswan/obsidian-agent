using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ObsidianAgent.Cli;
using OpenAI;
using Spectre.Console;

// ── Configuration ──────────────────────────────────────────────────────

bool verbose = args.Contains("--verbose");

string mcpEndpoint = Environment.GetEnvironmentVariable("MCP_ENDPOINT")
    ?? "http://localhost:5120";

string mcpProjectPath = Environment.GetEnvironmentVariable("MCP_PROJECT_PATH")
    ?? "src/ObsidianAgent.Mcp";

string aiEndpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT")
    ?? "http://localhost:12434/engines/v1";

string aiModel = Environment.GetEnvironmentVariable("AI_MODEL")
    ?? "ai/gpt-oss";

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.SetMinimumLevel(LogLevel.Warning));

// ── MCP Server ────────────────────────────────────────────────────────

await using McpServerManager serverManager = new();

bool serverReady = await serverManager.EnsureRunningAsync(mcpEndpoint, mcpProjectPath, verbose)
    .ConfigureAwait(false);

if (!serverReady) return;

// ── MCP Client ────────────────────────────────────────────────────────

var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri(mcpEndpoint),
    TransportMode = HttpTransportMode.AutoDetect
};

var transport = new HttpClientTransport(transportOptions, loggerFactory);

await using McpClient mcpClient = await McpClient
    .CreateAsync(transport, loggerFactory: loggerFactory)
    .ConfigureAwait(false);

IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

AnsiConsole.MarkupLine($"[dim]Loaded {mcpTools.Count} MCP tools.[/]");

string vaultName = VaultResolver.Resolve(mcpProjectPath);

// ── AI Chat Client (Docker Model Runner) ──────────────────────────────

IChatClient chatClient = new OpenAIClient(
        new ApiKeyCredential("not-needed-for-docker-model-runner"),
        new OpenAIClientOptions { Endpoint = new Uri(aiEndpoint) })
    .GetChatClient(aiModel)
    .AsIChatClient();

// ── Agent ─────────────────────────────────────────────────────────────

ChatClientAgent agent = chatClient.AsAIAgent(
    options: new ChatClientAgentOptions
    {
        Name = "obsidian",
        Description = "Obsidian vault management assistant",
        ChatOptions = new ChatOptions
        {
            Instructions = AgentInstructions.System,
            Tools = [.. mcpTools]
        }
    },
    loggerFactory: loggerFactory);

// ── Render Header ─────────────────────────────────────────────────────

Console.Clear();
AnsiConsole.WriteLine();

AnsiConsole.Write(
    new FigletText("Obsidian Agent")
        .Centered()
        .Color(Color.MediumPurple));

AnsiConsole.Write(
    new Rule($"[dim]Vault: [cyan]{Markup.Escape(vaultName)}[/][/]")
        .RuleStyle(Style.Parse("purple"))
        .Centered());

if (verbose)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(
        new Markup("[yellow bold]  VERBOSE MODE[/]")
            .Centered());
}

AnsiConsole.WriteLine();

AnsiConsole.Write(
    new Panel(
            new Markup(
                "[bold]How can I help you today?[/]\n" +
                "[dim]Ask me to manage notes, search your vault, track tasks, or work with daily notes.\n" +
                "For a full feature list or helpful integration ideas, just ask![/]\n\n" +
                "[dim]Type [cyan]:q[/], [cyan]quit[/], or [cyan]exit[/] to leave.[/]"))
        .Border(BoxBorder.Rounded)
        .BorderStyle(Style.Parse("purple"))
        .Header("[purple]Obsidian Agent[/]")
        .Expand());

AnsiConsole.WriteLine();

// ── Chat Loop ─────────────────────────────────────────────────────────

List<ChatMessage> messages = [];

try
{
    while (true)
    {
        AnsiConsole.Markup("[purple bold]>[/] ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            AnsiConsole.MarkupLine("[yellow]Message cannot be empty.[/]");
            continue;
        }

        if (input is ":q" or "quit" or "exit")
        {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, input));

        IAsyncEnumerable<AgentResponseUpdate> stream =
            agent.RunStreamingAsync(messages, session: null);

        (string responseText, List<string> toolCalls) =
            await ChatRenderer.RenderStreamingResponseAsync(stream).ConfigureAwait(false);

        if (toolCalls.Count > 0)
        {
            AnsiConsole.Write(
                new Panel(new Markup(string.Join("\n", toolCalls)))
                    .Header("[green]Tool Calls[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(Style.Parse("green dim"))
                    .Expand());
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            AnsiConsole.WriteLine();
            messages.Add(new ChatMessage(ChatRole.Assistant, responseText));
        }
    }
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[dim]Chat cancelled.[/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
}

// ── Cleanup ───────────────────────────────────────────────────────────

AnsiConsole.MarkupLine("[dim]Goodbye![/]");

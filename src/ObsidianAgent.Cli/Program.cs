using System.ClientModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenAI;
using Spectre.Console;
using Spectre.Console.Rendering;

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

// ── MCP Server Auto-Start ──────────────────────────────────────────────

Process? mcpServerProcess = null;
bool cliStartedServer = false;

bool serverRunning = await IsMcpServerReachableAsync(mcpEndpoint).ConfigureAwait(false);

if (!serverRunning)
{
    AnsiConsole.MarkupLine("[dim]MCP server not detected. Starting it automatically...[/]");

    mcpServerProcess = StartMcpServer(mcpProjectPath, verbose);

    if (mcpServerProcess is null)
    {
        AnsiConsole.MarkupLine("[red]Failed to start MCP server.[/]");
        return;
    }

    cliStartedServer = true;

    bool healthy = await WaitForMcpServerAsync(mcpEndpoint).ConfigureAwait(false);

    if (!healthy)
    {
        AnsiConsole.MarkupLine("[red]Timed out waiting for MCP server to start.[/]");
        await StopMcpServerAsync(mcpServerProcess).ConfigureAwait(false);
        return;
    }

    AnsiConsole.MarkupLine("[green]MCP server is running.[/]");
}
else
{
    AnsiConsole.MarkupLine("[green]MCP server already running.[/]");
}

// ── MCP Client ─────────────────────────────────────────────────────────

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

// Resolve the vault name from the same config chain the MCP server uses
string vaultName = ResolveVaultName(mcpProjectPath);

// ── AI Chat Client (Docker Model Runner) ───────────────────────────────

IChatClient chatClient = new OpenAIClient(
        new ApiKeyCredential("not-needed-for-docker-model-runner"),
        new OpenAIClientOptions { Endpoint = new Uri(aiEndpoint) })
    .GetChatClient(aiModel)
    .AsIChatClient();

// ── Agent ──────────────────────────────────────────────────────────────

const string instructions = """
    You are an Obsidian vault assistant. You help the user manage their
    vault using the full set of available tools — from basic note operations
    to knowledge graph navigation, task management, and daily workflows.

    Guidelines:
    - Be concise in your responses.
    - Show relative paths when referencing notes.
    - When creating notes, use proper markdown formatting.
    - Always confirm destructive operations (delete, overwrite) before executing.

    Tool Reference:

    Notes: Use ReadNote, CreateNote, UpdateNote, AppendToNote, DeleteNote,
    and ListNotes for core note management.

    Search: Use SearchNotes for file-level matches. Use SearchWithContext
    when the user needs to see matching lines in context.

    Knowledge Graph: Use GetBacklinks to find what links to a note, GetLinks
    to see outgoing links, FindOrphans for unlinked notes, FindDeadEnds for
    notes with no outgoing links, and FindUnresolved for broken wikilinks.

    Tasks: Use ListTasks to show tasks across the vault (filter with showDone
    or showTodo). Use UpdateTask to mark tasks done, todo, or toggle.

    Daily Notes: Use ReadDailyNote, AppendToDailyNote, PrependToDailyNote,
    and GetDailyNotePath for daily note operations.

    Properties & Tags: Use ReadProperty, SetProperty, RemoveProperty, and
    ListProperties to work with frontmatter metadata. Use ListTags and
    GetTag to explore the tag taxonomy.

    Templates: Use ListTemplates, ReadTemplate, and CreateFromTemplate to
    create notes from the user's established templates.

    Vault Intelligence: Use GetOutline for heading structure, ListBookmarks
    and AddBookmark for bookmarks, ListRecents for recently opened files,
    and GetVaultInfo for vault stats.

    Configuration: Use SetVault to get or switch the active vault.

    Integration Ideas (share these when the user asks for feature ideas or
    how to use the agent):

    - Morning Briefing: ReadDailyNote + ListTasks (todo) + ListRecents to
      build a daily summary, then AppendToDailyNote to write it.
    - Vault Health Check: FindOrphans + FindDeadEnds + FindUnresolved to
      audit knowledge graph quality and surface gaps.
    - Meeting Notes: CreateFromTemplate with a meeting template, SetProperty
      for metadata, then AppendToDailyNote to link it from today's note.
    - Research Capture: CreateFromTemplate for a research note, ListTags
      to pick consistent tags, GetBacklinks to find related notes.
    - Task Review: ListTasks across the vault, summarize open items, and
      AppendToDailyNote with the summary.
    - Cross-Vault Search: SetVault to switch vaults, SearchWithContext to
      find content, then switch back.
    """;

ChatClientAgent agent = chatClient.AsAIAgent(
    options: new ChatClientAgentOptions
    {
        Name = "obsidian",
        Description = "Obsidian vault management assistant",
        ChatOptions = new ChatOptions
        {
            Instructions = instructions,
            Tools = [.. mcpTools]
        }
    },
    loggerFactory: loggerFactory);

// ── Render Header ──────────────────────────────────────────────────────

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

// ── Chat Loop ──────────────────────────────────────────────────────────

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
            await RenderStreamingResponseAsync(stream).ConfigureAwait(false);

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

// ── Cleanup ────────────────────────────────────────────────────────────

AnsiConsole.MarkupLine("[dim]Goodbye![/]");

if (cliStartedServer && mcpServerProcess is not null)
{
    AnsiConsole.MarkupLine("[dim]Stopping MCP server...[/]");
    await StopMcpServerAsync(mcpServerProcess).ConfigureAwait(false);
    AnsiConsole.MarkupLine("[green]MCP server stopped.[/]");
}

// ── Helper Methods ─────────────────────────────────────────────────────

static async Task<bool> IsMcpServerReachableAsync(string endpoint)
{
    try
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
        await client.GetAsync(new Uri(endpoint)).ConfigureAwait(false);
        return true;
    }
    catch
    {
        return false;
    }
}

static Process? StartMcpServer(string projectPath, bool verbose)
{
    string separator = verbose ? " -- --verbose" : "";

    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{projectPath}\"{separator}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    return Process.Start(startInfo);
}

static async Task<bool> WaitForMcpServerAsync(string endpoint)
{
    return await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("purple"))
        .StartAsync("Starting MCP server...", async ctx =>
        {
            using CancellationTokenSource timeoutCts = new(TimeSpan.FromMinutes(2));

            while (!timeoutCts.Token.IsCancellationRequested)
            {
                bool reachable = await IsMcpServerReachableAsync(endpoint).ConfigureAwait(false);

                if (reachable)
                {
                    return true;
                }

                ctx.Status("Waiting for MCP server to become healthy...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return false;
        }).ConfigureAwait(false);
}

static async Task StopMcpServerAsync(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
    }
    catch (InvalidOperationException)
    {
        // Process already exited
    }
    finally
    {
        process.Dispose();
    }
}

static async Task<(string ResponseText, List<string> ToolCalls)> RenderStreamingResponseAsync(
    IAsyncEnumerable<AgentResponseUpdate> responseStream)
{
    StringBuilder responseBuilder = new();
    StringBuilder reasoningBuilder = new();
    List<string> toolCallOutputs = [];

    IAsyncEnumerator<AgentResponseUpdate> enumerator =
        responseStream.GetAsyncEnumerator();

    try
    {
        bool hasMore = true;

        // Phase 1: Thinking spinner until first text token
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Thinking...", async ctx =>
            {
                while (hasMore)
                {
                    hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    if (!hasMore) break;

                    bool gotText = false;
                    foreach (AIContent content in enumerator.Current.Contents)
                    {
                        if (content is TextContent textContent)
                        {
                            responseBuilder.Append(textContent.Text);
                            gotText = true;
                        }
                        else if (content is TextReasoningContent reasoningContent)
                        {
                            reasoningBuilder.Append(reasoningContent.Text);
                            string truncated = TruncateFromEnd(reasoningBuilder.ToString(), 80);
                            ctx.Status($"Thinking... {Markup.Escape(truncated)}");
                        }
                        else
                        {
                            ProcessNonTextContent(content, toolCallOutputs);
                        }
                    }

                    if (gotText) break;
                }
            }).ConfigureAwait(false);

        // Phase 2: Live streaming text
        await AnsiConsole.Live(BuildResponseDisplay(reasoningBuilder, responseBuilder))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                while (hasMore)
                {
                    hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    if (!hasMore) break;

                    bool needsRefresh = false;
                    foreach (AIContent content in enumerator.Current.Contents)
                    {
                        if (content is TextContent textContent)
                        {
                            responseBuilder.Append(textContent.Text);
                            needsRefresh = true;
                        }
                        else if (content is TextReasoningContent reasoningContent)
                        {
                            reasoningBuilder.Append(reasoningContent.Text);
                            needsRefresh = true;
                        }
                        else
                        {
                            ProcessNonTextContent(content, toolCallOutputs);
                        }
                    }

                    if (needsRefresh)
                    {
                        ctx.UpdateTarget(BuildResponseDisplay(reasoningBuilder, responseBuilder));
                        ctx.Refresh();
                    }
                }
            }).ConfigureAwait(false);
    }
    finally
    {
        await enumerator.DisposeAsync().ConfigureAwait(false);
    }

    AnsiConsole.WriteLine();

    return (responseBuilder.ToString(), toolCallOutputs);
}

static IRenderable BuildResponseDisplay(StringBuilder reasoningBuilder, StringBuilder responseBuilder)
{
    Panel responsePanel = new Panel(Markup.Escape(responseBuilder.ToString()))
        .Header("[green]Assistant[/]")
        .Border(BoxBorder.Rounded)
        .BorderStyle(Style.Parse("green"))
        .Expand();

    if (reasoningBuilder.Length == 0)
    {
        return responsePanel;
    }

    Panel reasoningPanel = new Panel(
            new Markup($"[dim italic]{Markup.Escape(reasoningBuilder.ToString())}[/]"))
        .Header("[dim]Thinking[/]")
        .Border(BoxBorder.Rounded)
        .BorderStyle(Style.Parse("grey"))
        .Expand();

    return new Rows(reasoningPanel, responsePanel);
}

static string TruncateFromEnd(string text, int maxLength)
{
    string normalized = text.ReplaceLineEndings(" ");
    return normalized.Length <= maxLength
        ? normalized
        : "..." + normalized[^(maxLength - 3)..];
}

static void ProcessNonTextContent(AIContent content, List<string> toolCallOutputs)
{
    switch (content)
    {
        case FunctionCallContent functionCallContent:
            string args = FormatArguments(functionCallContent.Arguments);
            toolCallOutputs.Add($"[green]> {Markup.Escape(functionCallContent.Name)}[/]({Markup.Escape(args)})");
            break;

        case FunctionResultContent functionResultContent:
            if (functionResultContent.Exception is not null)
            {
                toolCallOutputs.Add(
                    $"[red]Error: {Markup.Escape(functionResultContent.Exception.Message)}[/]");
            }
            else if (functionResultContent.Result is not null)
            {
                string resultText = functionResultContent.Result.ToString() ?? "";
                if (resultText.Length > 200)
                {
                    resultText = resultText[..200] + "...";
                }
                toolCallOutputs.Add(
                    $"[dim]  Result: {Markup.Escape(resultText)}[/]");
            }
            break;
    }
}

static string FormatArguments(IDictionary<string, object?>? arguments)
{
    if (arguments is null || arguments.Count == 0) return "";
    return string.Join(", ", arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
}

static string ResolveVaultName(string mcpProjectPath)
{
    const string defaultVault = "sample-vault";

    // Check user settings first (~/.obsidian-agent/settings.json)
    try
    {
        string userSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".obsidian-agent", "settings.json");

        if (File.Exists(userSettingsPath))
        {
            string json = File.ReadAllText(userSettingsPath);
            var match = System.Text.RegularExpressions.Regex.Match(json, "\"vaultName\"\\s*:\\s*\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
            var match = System.Text.RegularExpressions.Regex.Match(json, "\"VaultName\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success) return match.Groups[1].Value;
        }
    }
    catch { }

    return defaultVault;
}

using System.ClientModel;
using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace ObsidianAgent.Cli;

public sealed record AgentHandle(AIAgent Agent, IAsyncDisposable? Lifetime) : IAsyncDisposable
{
    public ValueTask DisposeAsync() =>
        Lifetime?.DisposeAsync() ?? ValueTask.CompletedTask;
}

public static class AgentFactory
{
    private const string AgentName = "obsidian";
    private const string AgentDescription = "Obsidian vault management assistant";

    public static AgentHandle CreateDocker(
        string endpoint,
        string model,
        IList<AITool> tools,
        string instructions,
        ILoggerFactory loggerFactory)
    {
        IChatClient chatClient = new OpenAIClient(
                new ApiKeyCredential("not-needed-for-docker-model-runner"),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
            .GetChatClient(model)
            .AsIChatClient();

        ChatClientAgent agent = chatClient.AsAIAgent(
            options: new ChatClientAgentOptions
            {
                Name = AgentName,
                Description = AgentDescription,
                ChatOptions = new ChatOptions
                {
                    Instructions = instructions,
                    Tools = tools
                }
            },
            loggerFactory: loggerFactory);

        return new AgentHandle(agent, Lifetime: null);
    }

    public static async Task<AgentHandle> CreateCopilotAsync(
        string? model,
        IList<AITool> tools,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        string? cliPath = ResolveCopilotCliPath();

        CopilotClient client = cliPath is not null
            ? new CopilotClient(new CopilotClientOptions { CliPath = cliPath })
            : new CopilotClient();

        await client.StartAsync(cancellationToken).ConfigureAwait(false);

        string copilotInstructions = $"{instructions}\n\n{AgentInstructions.CopilotToolGuardrails}";

        SessionConfig sessionConfig = new()
        {
            Tools = tools.OfType<AIFunction>().ToList(),
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = copilotInstructions
            },
            OnPermissionRequest = PermissionHandler.ApproveAll
        };

        if (!string.IsNullOrWhiteSpace(model))
        {
            sessionConfig.Model = model;
        }

        AIAgent agent = client.AsAIAgent(
            sessionConfig,
            ownsClient: true,
            name: AgentName,
            description: AgentDescription);

        return new AgentHandle(agent, (IAsyncDisposable)agent);
    }

    private static string? ResolveCopilotCliPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("COPILOT_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        string exeName = OperatingSystem.IsWindows() ? "copilot.exe" : "copilot";

        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;

            string candidate = Path.Combine(dir, exeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

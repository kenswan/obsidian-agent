using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ObsidianAgent.Cli;

public static class ChatRenderer
{
    public static async Task<(string ResponseText, List<string> ToolCalls)> RenderStreamingResponseAsync(
        IAsyncEnumerable<AgentResponseUpdate> responseStream,
        bool verbose = false)
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
                                ProcessNonTextContent(content, toolCallOutputs, verbose);
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
                                ProcessNonTextContent(content, toolCallOutputs, verbose);
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

    private static IRenderable BuildResponseDisplay(StringBuilder reasoningBuilder, StringBuilder responseBuilder)
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

    private static string TruncateFromEnd(string text, int maxLength)
    {
        string normalized = text.ReplaceLineEndings(" ");
        return normalized.Length <= maxLength
            ? normalized
            : "..." + normalized[^(maxLength - 3)..];
    }

    private static void ProcessNonTextContent(AIContent content, List<string> toolCallOutputs, bool verbose)
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
                else if (verbose && functionResultContent.Result is not null)
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

    private static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0) return "";
        return string.Join(", ", arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}

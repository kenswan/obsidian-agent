# ObsidianAgent.ClaudeCli

TypeScript chat CLI that talks to your Obsidian vault through the same `ObsidianAgent.Mcp` server the .NET CLI uses — but driven by **Anthropic's Claude** via the [Claude Agent SDK](https://www.npmjs.com/package/@anthropic-ai/claude-agent-sdk) instead of a local Docker Model Runner.

Companion sample to [`ObsidianAgent.Cli`](../ObsidianAgent.Cli/). Same MCP server, same 34 tools, different LLM.

## Prerequisites

- **Node.js 20+**
- **.NET 10 SDK** (to run the MCP server — this CLI will auto-start it if it is not already up)
- **Obsidian CLI** on `PATH` (the MCP server shells out to it)
- **Claude authentication** (one of the following):
  - `ANTHROPIC_API_KEY` exported in your shell, **or**
  - A logged-in Claude Code install (the SDK's bundled runtime reads OAuth credentials from `~/.claude/`, the same place `claude login` writes them)

## Install & run

```bash
cd src/ObsidianAgent.ClaudeCli
npm install
# Either export an API key...
export ANTHROPIC_API_KEY=sk-ant-...
# ...or skip it entirely if you have an authenticated `claude` install.
npm run dev
```

On start, the CLI:
1. Pings `http://localhost:5120` (the MCP server). If it is unreachable, spawns `dotnet run --project ../ObsidianAgent.Mcp` and waits for it to come up.
2. Renders the Figlet header and vault name.
3. Drops into an interactive prompt. Type `:q`, `quit`, or `exit` to leave.

On exit, the CLI kills the MCP server only if it was the one that started it.

## Environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `ANTHROPIC_API_KEY` | _(optional)_ | Claude API key. If unset, the SDK falls back to Claude Code session credentials. |
| `CLAUDE_MODEL` | SDK default | Override the Claude model |
| `MCP_ENDPOINT` | `http://localhost:5120` | MCP server URL |
| `MCP_PROJECT_PATH` | `../ObsidianAgent.Mcp` | Project path used when auto-starting the server |

Copy `.env.example` and source it if you prefer not to export by hand.

## How the port works

The MCP server is language-agnostic HTTP, so nothing on that side changes. The SDK is configured like this:

```ts
query({
  prompt,
  options: {
    systemPrompt: AGENT_INSTRUCTIONS,
    mcpServers: { obsidian: { type: "http", url: mcpEndpoint } },
    tools: [],                      // disable every built-in Claude Code tool
    permissionMode: "bypassPermissions",
    includePartialMessages: true,   // token-by-token streaming
    resume: sessionId,              // preserve history across turns
  },
});
```

`tools: []` scopes the agent to only the MCP server's `mcp__obsidian__*` tools — no filesystem, no Bash, no web access.

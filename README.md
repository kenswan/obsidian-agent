# Obsidian Agent

A sandbox example of using local AI models to manage an [Obsidian](https://obsidian.md) vault through the [Model Context Protocol (MCP)](https://modelcontextprotocol.io). Built with [Microsoft Agent Framework](https://github.com/microsoft/agents), [Docker Model Runner](https://docs.docker.com/desktop/features/model-runner/), and [Spectre.Console](https://spectreconsole.net).

## Architecture

```
CLI (Spectre.Console)  ──MCP Client──▶  MCP Server (ASP.NET Core HttpTransport)
       │                                       │
       │ IChatClient                           │ Vault tools (read/create/search/etc.)
       ▼                                       ▼
  Docker Model Runner                    Obsidian Vault (filesystem)
  (localhost:12434)                      (sample-vault/)
```

- **ObsidianAgent.Cli** — Interactive terminal chat powered by Spectre.Console. Connects to the MCP server as an agent tool and uses Docker Model Runner for AI inference. Auto-starts the MCP server if it's not already running.
- **ObsidianAgent.Mcp** — ASP.NET Core app exposing 34 Obsidian vault operations (notes, search, graph navigation, tasks, daily notes, properties, tags, templates, and vault intelligence) as MCP tools over HTTP.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with **Docker Model Runner** enabled
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Obsidian](https://obsidian.md/) desktop app (must be running for CLI access)

### Pull an AI model

```bash
docker model pull ai/mistral-small
```

## Quick Start

```bash
dotnet restore
dotnet run --project src/ObsidianAgent.Cli
```

Or use the convenience script, which runs from the repo root and suppresses `dotnet` build output by default:

```bash
./scripts/start.sh
```

The CLI will auto-detect that the MCP server isn't running, start it in the background, and shut it down when you exit.

### Verbose Mode

Pass `--verbose` to surface MCP tool calls, results, and build output for debugging:

```bash
dotnet run --project src/ObsidianAgent.Cli -- --verbose
./scripts/start.sh --verbose
```

When enabled, the banner displays a `VERBOSE MODE` indicator and tool invocations are rendered inline with each streamed response.

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `AI_ENDPOINT` | `http://localhost:12434/engines/v1` | Docker Model Runner endpoint |
| `AI_MODEL` | `ai/gpt-oss` | Model to use for inference |
| `MCP_ENDPOINT` | `http://localhost:5120` | MCP server endpoint |
| `MCP_PROJECT_PATH` | `src/ObsidianAgent.Mcp` | Path to MCP server project (for auto-start) |

### Vault Path

The MCP server points to `sample-vault/` by default. To use your own Obsidian vault, edit `src/ObsidianAgent.Mcp/appsettings.json`:

```json
{
  "Obsidian": {
    "VaultName": "My Vault",
    "VaultPath": "/path/to/your/vault"
  }
}
```

## Sample Vault

A demo vault is included at `sample-vault/` with notes demonstrating tags, wikilinks, frontmatter, and folder organization. This lets you try the agent immediately without connecting a real vault.

## MCP Tools

34 tools organized across 9 domains. See [Features.md](Features.md) for detailed descriptions and integration workflows.

| Domain | Tools |
|--------|-------|
| **Notes** | `ReadNote`, `CreateNote`, `UpdateNote`, `DeleteNote`, `AppendToNote`, `ListNotes` |
| **Search** | `SearchNotes`, `SearchWithContext` |
| **Knowledge Graph** | `GetBacklinks`, `GetLinks`, `FindOrphans`, `FindDeadEnds`, `FindUnresolved` |
| **Tasks** | `ListTasks`, `UpdateTask` |
| **Daily Notes** | `ReadDailyNote`, `AppendToDailyNote`, `PrependToDailyNote`, `GetDailyNotePath` |
| **Properties & Tags** | `ListProperties`, `ReadProperty`, `SetProperty`, `RemoveProperty`, `ListTags`, `GetTag` |
| **Templates** | `ListTemplates`, `ReadTemplate`, `CreateFromTemplate` |
| **Vault Intelligence** | `GetOutline`, `ListBookmarks`, `AddBookmark`, `ListRecents`, `GetVaultInfo` |
| **Configuration** | `SetVault` |

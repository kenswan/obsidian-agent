# Obsidian Agent

A sandbox example of using local AI models to manage an [Obsidian](https://obsidian.md) vault through the [Model Context Protocol (MCP)](https://modelcontextprotocol.io). Built with [Microsoft Agent Framework](https://github.com/microsoft/agents), [Docker Model Runner](https://docs.docker.com/desktop/features/model-runner/), and [Spectre.Console](https://spectreconsole.net).

## Architecture

```
CLI (Spectre.Console)  ──MCP Client──▶  MCP Server (ASP.NET Core HttpTransport)
       │                                       │
       │ AIAgent                               │ Vault tools (read/create/search/etc.)
       ▼                                       ▼
  Docker Model Runner     ─ or ─    Obsidian Vault (filesystem)
  (localhost:12434)                     (sample-vault/)
  GitHub Copilot CLI
  (via --copilot flag)
```

- **ObsidianAgent.Cli** — Interactive terminal chat powered by Spectre.Console. Connects to the MCP server as an agent tool. Uses Docker Model Runner by default, or the GitHub Copilot CLI when invoked with `--copilot`. Auto-starts the MCP server if it's not already running.
- **ObsidianAgent.Mcp** — ASP.NET Core app exposing 34 Obsidian vault operations (notes, search, graph navigation, tasks, daily notes, properties, tags, templates, and vault intelligence) as MCP tools over HTTP.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Obsidian](https://obsidian.md/) desktop app (must be running for CLI access)
- One of the following AI backends:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop/) with **Docker Model Runner** enabled (default)
  - [GitHub Copilot CLI](https://github.com/github/copilot-sdk) installed and authenticated (required only when using `--copilot`)

### Pull an AI model (Docker Model Runner backend)

```bash
docker model pull ai/mistral-small
```

## Quick Start

```bash
dotnet restore
dotnet run --project src/ObsidianAgent.Cli
```

The CLI will auto-detect that the MCP server isn't running, start it in the background, and shut it down when you exit.

## Configuration

### CLI Flags

| Flag | Description |
|------|-------------|
| `--verbose` | Enable verbose logging |
| `--copilot [model]` | Use GitHub Copilot as the AI backend. Defaults to `gpt-4.1` (lowest token spend); pass a model (e.g. `claude-opus-4.5`) to override. |

### Environment Variables (Docker Model Runner backend)

| Variable | Default | Description |
|----------|---------|-------------|
| `AI_ENDPOINT` | `http://localhost:12434/engines/v1` | Docker Model Runner endpoint |
| `AI_MODEL` | `ai/gpt-oss` | Model to use for inference |
| `MCP_ENDPOINT` | `http://localhost:5120` | MCP server endpoint |
| `MCP_PROJECT_PATH` | `src/ObsidianAgent.Mcp` | Path to MCP server project (for auto-start) |

### GitHub Copilot backend

When `--copilot` is passed, the CLI uses the [GitHub Copilot CLI](https://github.com/github/copilot-sdk) instead of Docker Model Runner. The Copilot CLI must be installed and authenticated on your machine beforehand — this project does not manage that setup. Tool calls are auto-approved (the `--copilot` flag itself is the trust boundary); the agent is instruction-hardened to prefer the MCP vault tools over Copilot's native shell/file tools.

```bash
./scripts/start-copilot.sh                   # defaults to gpt-4.1
./scripts/start-copilot.sh claude-opus-4.5   # override model
./scripts/start.sh --copilot                 # equivalent to the shortcut above
```

This project pins `GitHub.Copilot.SDK` to `0.2.2` (JSON-RPC protocol v3). Your local Copilot CLI must speak the same protocol — tested with CLI `v1.0.32`. Older CLIs that speak protocol v2 will report a version mismatch at startup.

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

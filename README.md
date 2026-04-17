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
- **ObsidianAgent.Mcp** — ASP.NET Core app exposing Obsidian vault operations (read, create, update, delete, search notes) as MCP tools over HTTP.

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

The CLI will auto-detect that the MCP server isn't running, start it in the background, and shut it down when you exit.

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

| Tool | Description |
|------|-------------|
| `ReadNote` | Read the content of a note |
| `CreateNote` | Create a new note |
| `UpdateNote` | Overwrite an existing note |
| `DeleteNote` | Delete a note |
| `AppendToNote` | Append content to an existing note |
| `ListNotes` | List notes in the vault or a subfolder |
| `SearchNotes` | Full-text search across all notes |

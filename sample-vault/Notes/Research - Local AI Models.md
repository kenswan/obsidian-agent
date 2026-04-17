---
created: 2026-04-10
updated: 2026-04-17
category: research
---

# Research: Local AI Models

Exploring local AI model inference for developer tooling and knowledge management.

## Docker Model Runner

Docker Desktop includes a built-in model runner that serves OpenAI-compatible endpoints locally:

- **Endpoint**: `http://localhost:12434/engines/v1`
- **No API key needed** — runs entirely on-device
- **Models**: Supports GGUF format models (Llama, Mistral, Phi, etc.)
- **Privacy**: All data stays local, no cloud round-trips

### Setup

```bash
# Pull a model
docker model pull ai/mistral-small

# Verify it's running
curl http://localhost:12434/engines/v1/models
```

## Model Context Protocol (MCP)

MCP provides a standardized way to expose tools to AI models:

- **Server**: Hosts tools with typed schemas (via `[McpServerTool]` attributes)
- **Client**: Discovers and invokes tools dynamically
- **Transport**: HTTP (Streamable HTTP / SSE) or stdio

This enables AI agents to interact with external systems (vaults, APIs, databases) through well-defined tool interfaces.

## Microsoft Agent Framework

The `Microsoft.Agents.AI` package provides:

- `IChatClient` — abstraction over any AI provider
- `AIAgent` — agent with instructions, tools, and streaming support
- `ChatClientAgentOptions` — configuration for agent behavior
- Works seamlessly with MCP tools via `McpClientTool` → `AITool` casting

## Integration Pattern

```
User → CLI (Spectre.Console) → Agent (Microsoft.Agents.AI)
                                  ├── ChatClient (Docker Model Runner)
                                  └── Tools (MCP Client → MCP Server → Obsidian Vault)
```

## Related

- [[API Migration]] — uses similar architecture patterns
- [[Website Redesign]] — frontend will consume AI-assisted content

#research #ai #mcp #docker #local-first

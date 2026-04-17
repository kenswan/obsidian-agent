#!/bin/bash
set -e

# Navigate to repo root (parent of scripts/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

echo "Starting Obsidian Agent CLI..."
echo "The CLI will auto-start the MCP server if needed."
echo ""

dotnet run --project src/ObsidianAgent.Cli

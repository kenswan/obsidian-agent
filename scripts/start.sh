#!/bin/bash
set -e

# Navigate to repo root (parent of scripts/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

echo "Starting Obsidian Agent CLI..."
echo "The CLI will auto-start the MCP server if needed."
echo ""

DOTNET_VERBOSITY="quiet"
for arg in "$@"; do
    if [ "$arg" = "--verbose" ]; then
        DOTNET_VERBOSITY="minimal"
        break
    fi
done

dotnet run --project src/ObsidianAgent.Cli --verbosity "$DOTNET_VERBOSITY" -- "$@"

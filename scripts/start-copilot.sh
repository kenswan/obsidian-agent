#!/bin/bash
set -e

# Navigate to repo root (parent of scripts/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

echo "Starting Obsidian Agent CLI (GitHub Copilot backend)..."
echo "Requires GitHub Copilot CLI to be installed and authenticated."
echo ""

if [ -z "$COPILOT_CLI_PATH" ]; then
    COPILOT_CLI_PATH="$(command -v copilot || true)"
fi

if [ -z "$COPILOT_CLI_PATH" ]; then
    echo "Error: 'copilot' CLI not found on PATH. Install from https://github.com/github/copilot-sdk or set COPILOT_CLI_PATH." >&2
    exit 1
fi

export COPILOT_CLI_PATH

dotnet run --project src/ObsidianAgent.Cli -- --copilot "$@"

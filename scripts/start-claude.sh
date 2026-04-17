#!/bin/bash
set -e

# Navigate to repo root (parent of scripts/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CLI_DIR="$REPO_ROOT/src/ObsidianAgent.ClaudeCli"

cd "$CLI_DIR"

if [ ! -d "node_modules" ]; then
  echo "Installing dependencies (first run)..."
  npm install
  echo ""
fi

echo "Starting Obsidian Agent CLI (Claude)..."
echo "The CLI will auto-start the MCP server if needed."
echo ""

npm run dev -- "$@"

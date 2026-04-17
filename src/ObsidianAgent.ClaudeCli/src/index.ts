import * as readline from "node:readline/promises";
import { stdin as input, stdout as output } from "node:process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import path from "node:path";
import boxen from "boxen";
import chalk from "chalk";
import figlet from "figlet";
import { query, type SDKMessage, type SDKUserMessage } from "@anthropic-ai/claude-agent-sdk";
import { AGENT_INSTRUCTIONS } from "./agentInstructions.js";
import { McpServerManager } from "./mcpServerManager.js";
import { renderStreamingResponse, renderToolCallsPanel } from "./chatRenderer.js";

const verbose = process.argv.includes("--verbose");

const mcpEndpoint = process.env.MCP_ENDPOINT ?? "http://localhost:5120";
const mcpProjectPath = process.env.MCP_PROJECT_PATH ?? path.resolve(process.cwd(), "../ObsidianAgent.Mcp");
const claudeModel = process.env.CLAUDE_MODEL;

if (!process.env.ANTHROPIC_API_KEY) {
  console.log(
    chalk.dim(
      "ANTHROPIC_API_KEY is not set. Falling back to Claude Code session credentials (~/.claude/).",
    ),
  );
}

const serverManager = new McpServerManager();

async function main(): Promise<void> {
  const serverReady = await serverManager.ensureRunning(mcpEndpoint, mcpProjectPath, verbose);
  if (!serverReady) return;

  const vaultName = resolveVaultName(mcpProjectPath);

  renderHeader(vaultName, verbose);

  const rl = readline.createInterface({ input, output });

  // Node 25's `beforeExit` detection considers a readline-waiting-on-stdin
  // process to have no pending work and terminates mid-session. A no-op
  // interval keeps the event loop alive until we explicitly break out.
  const keepalive = setInterval(() => {}, 1 << 30);

  // Interactive multi-turn chat uses streaming input: one `query()` call with
  // an AsyncIterable<SDKUserMessage> that yields each user turn. The SDK keeps
  // a single subprocess alive across turns instead of spawning a new one per
  // message (which doesn't reliably resume prior conversation state).
  const inputQueue: SDKUserMessage[] = [];
  let resolveWaiter: ((msg: SDKUserMessage) => void) | null = null;
  let inputClosed = false;

  async function* promptStream(): AsyncGenerator<SDKUserMessage> {
    while (!inputClosed) {
      if (inputQueue.length > 0) {
        yield inputQueue.shift()!;
        continue;
      }
      const next = await new Promise<SDKUserMessage | null>((resolve) => {
        resolveWaiter = resolve;
      });
      if (next === null) return;
      yield next;
    }
  }

  function submitUserMessage(text: string): void {
    const msg: SDKUserMessage = {
      type: "user",
      message: { role: "user", content: text },
      parent_tool_use_id: null,
    };
    if (resolveWaiter) {
      const r = resolveWaiter;
      resolveWaiter = null;
      r(msg);
    } else {
      inputQueue.push(msg);
    }
  }

  function closeInput(): void {
    inputClosed = true;
    if (resolveWaiter) {
      const r = resolveWaiter;
      resolveWaiter = null;
      r(null as unknown as SDKUserMessage);
    }
  }

  const response = query({
    prompt: promptStream(),
    options: {
      ...(claudeModel ? { model: claudeModel } : {}),
      systemPrompt: AGENT_INSTRUCTIONS,
      mcpServers: {
        obsidian: { type: "http", url: mcpEndpoint },
      },
      tools: [],
      permissionMode: "bypassPermissions",
      allowDangerouslySkipPermissions: true,
      includePartialMessages: true,
    },
  });

  const responseIterator = response[Symbol.asyncIterator]();

  // Yields messages from the shared stream until a `result` message arrives,
  // then returns — delimiting one assistant turn for the renderer.
  async function* turnStream(): AsyncGenerator<SDKMessage> {
    while (true) {
      const { done, value } = await responseIterator.next();
      if (done) return;
      yield value;
      if (value.type === "result") return;
    }
  }

  try {
    while (true) {
      const line = await rl.question(chalk.magentaBright.bold("> "));
      const trimmed = line.trim();

      if (trimmed === "") {
        console.log(chalk.yellow("Message cannot be empty."));
        continue;
      }

      if (trimmed === ":q" || trimmed === "quit" || trimmed === "exit") {
        break;
      }

      submitUserMessage(trimmed);

      try {
        const result = await renderStreamingResponse(turnStream());
        renderToolCallsPanel(result.toolCalls);
        output.write("\n");
      } catch (err) {
        console.log(chalk.red(`Error: ${(err as Error).message}`));
      }
    }
  } finally {
    closeInput();
    response.close();
    clearInterval(keepalive);
    rl.close();
  }

  console.log(chalk.dim("Goodbye!"));
}

function renderHeader(vaultName: string, verbose: boolean): void {
  console.clear();
  console.log();

  const title = figlet.textSync("Obsidian Agent", { horizontalLayout: "default" });
  const centered = centerBlock(title, process.stdout.columns);
  console.log(chalk.hex("#9370DB")(centered));

  const rule = centerLine(`Vault: ${chalk.cyan(vaultName)}`, process.stdout.columns);
  console.log(chalk.magenta(rule));

  if (verbose) {
    console.log();
    console.log(centerLine(chalk.yellow.bold("VERBOSE MODE"), process.stdout.columns));
  }

  console.log();

  const panel = boxen(
    chalk.bold("How can I help you today?") +
      "\n" +
      chalk.dim(
        "Ask me to manage notes, search your vault, track tasks, or work with daily notes.\n" +
          "For a full feature list or helpful integration ideas, just ask!",
      ) +
      "\n\n" +
      chalk.dim(`Type ${chalk.cyan(":q")}, ${chalk.cyan("quit")}, or ${chalk.cyan("exit")} to leave.`),
    {
      title: chalk.magenta("Obsidian Agent (Claude)"),
      titleAlignment: "left",
      borderStyle: "round",
      borderColor: "magenta",
      padding: { top: 0, bottom: 0, left: 1, right: 1 },
      width: process.stdout.columns,
    },
  );
  console.log(panel);
  console.log();
}

function centerBlock(block: string, width: number): string {
  return block
    .split("\n")
    .map((line) => centerLine(line, width))
    .join("\n");
}

function centerLine(line: string, width: number): string {
  const visibleLength = line.replace(/\x1b\[[0-9;]*m/g, "").length;
  if (visibleLength >= width) return line;
  const pad = Math.floor((width - visibleLength) / 2);
  return " ".repeat(pad) + line;
}

function resolveVaultName(projectPath: string): string {
  const DEFAULT = "sample-vault";
  try {
    const userSettingsPath = path.join(homedir(), ".obsidian-agent", "settings.json");
    if (existsSync(userSettingsPath)) {
      const json = readFileSync(userSettingsPath, "utf8");
      const match = json.match(/"vaultName"\s*:\s*"([^"]+)"/i);
      if (match) return match[1];
    }
  } catch {
    // fall through
  }

  try {
    const appSettingsPath = path.join(projectPath, "appsettings.json");
    if (existsSync(appSettingsPath)) {
      const json = readFileSync(appSettingsPath, "utf8");
      const match = json.match(/"VaultName"\s*:\s*"([^"]+)"/);
      if (match) return match[1];
    }
  } catch {
    // fall through
  }

  return DEFAULT;
}

const shutdown = async (): Promise<void> => {
  await serverManager.dispose();
  process.exit(0);
};

process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);

// Avoid top-level `await` here: Node 25+ flags top-level awaits that resolve
// only after long-running async work (like an interactive readline loop) as
// "unsettled" and terminates the process. A .then/.catch chain sidesteps that.
main()
  .catch((err) => {
    console.log(chalk.red(`Fatal: ${(err as Error).message}`));
  })
  .finally(() => serverManager.dispose());

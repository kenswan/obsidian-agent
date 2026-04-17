import type { SDKMessage } from "@anthropic-ai/claude-agent-sdk";
import boxen from "boxen";
import chalk from "chalk";
import logUpdate from "log-update";
import ora, { type Ora } from "ora";

export interface RenderResult {
  responseText: string;
  toolCalls: string[];
  sessionId: string | undefined;
}

export async function renderStreamingResponse(
  stream: AsyncIterable<SDKMessage>,
): Promise<RenderResult> {
  const toolCalls: string[] = [];
  let responseText = "";
  let sessionId: string | undefined;
  let spinner: Ora | null = ora({ text: "Thinking...", color: "magenta" }).start();
  let livePanelActive = false;

  const stopSpinner = () => {
    if (spinner) {
      spinner.stop();
      spinner.clear();
      spinner = null;
    }
  };

  const renderLivePanel = () => {
    logUpdate(buildAssistantPanel(responseText));
    livePanelActive = true;
  };

  try {
    for await (const message of stream) {
      if (message.type === "system" && message.subtype === "init") {
        sessionId = message.session_id;
        continue;
      }

      if (message.type === "stream_event") {
        const event = message.event;
        if (event.type === "content_block_delta" && event.delta?.type === "text_delta") {
          if (spinner) stopSpinner();
          responseText += event.delta.text;
          renderLivePanel();
        }
        continue;
      }

      if (message.type === "assistant") {
        const content = message.message.content;
        if (Array.isArray(content)) {
          for (const block of content) {
            if (block.type === "tool_use") {
              const args = formatArguments(block.input);
              toolCalls.push(chalk.green(`> ${block.name}`) + chalk.reset(`(${args})`));
              if (spinner) spinner.text = `Calling ${block.name}...`;
            }
          }
        }
        continue;
      }

      if (message.type === "user") {
        const content = message.message.content;
        if (Array.isArray(content)) {
          for (const block of content) {
            if (typeof block === "object" && block !== null && "type" in block && block.type === "tool_result") {
              const resultText = stringifyToolResult(block);
              if (resultText) {
                toolCalls.push(chalk.dim(`  Result: ${resultText}`));
              }
              if (spinner) spinner.text = "Thinking...";
            }
          }
        }
        continue;
      }

      if (message.type === "result") {
        // Result messages include the final aggregated text. If the live panel
        // never activated (e.g., no stream_event because includePartialMessages
        // was disabled), use result.result as the response text.
        if (!responseText && message.subtype === "success" && message.result) {
          responseText = message.result;
        }
        continue;
      }
    }
  } finally {
    stopSpinner();
    if (livePanelActive) {
      logUpdate(buildAssistantPanel(responseText));
      logUpdate.done();
    } else if (responseText) {
      process.stdout.write(buildAssistantPanel(responseText) + "\n");
    }
  }

  return { responseText, toolCalls, sessionId };
}

export function renderToolCallsPanel(toolCalls: string[]): void {
  if (toolCalls.length === 0) return;

  const body = toolCalls.join("\n");
  const panel = boxen(body, {
    title: chalk.green("Tool Calls"),
    titleAlignment: "left",
    borderStyle: "round",
    borderColor: "green",
    padding: { top: 0, bottom: 0, left: 1, right: 1 },
    width: process.stdout.columns,
  });
  process.stdout.write(panel + "\n");
}

function buildAssistantPanel(text: string): string {
  return boxen(text || chalk.dim("(no content)"), {
    title: chalk.green("Assistant"),
    titleAlignment: "left",
    borderStyle: "round",
    borderColor: "green",
    padding: { top: 0, bottom: 0, left: 1, right: 1 },
    width: process.stdout.columns,
  });
}

function formatArguments(input: unknown): string {
  if (!input || typeof input !== "object") return "";
  const entries = Object.entries(input as Record<string, unknown>);
  if (entries.length === 0) return "";
  return entries
    .map(([k, v]) => `${k}: ${truncate(String(v), 60)}`)
    .join(", ");
}

function stringifyToolResult(block: unknown): string {
  if (typeof block !== "object" || block === null) return "";
  const content = (block as { content?: unknown }).content;
  let text = "";

  if (typeof content === "string") {
    text = content;
  } else if (Array.isArray(content)) {
    for (const part of content) {
      if (typeof part === "object" && part !== null && "type" in part && (part as { type: string }).type === "text") {
        text += (part as { text?: string }).text ?? "";
      }
    }
  }

  return truncate(text.trim(), 200);
}

function truncate(text: string, maxLength: number): string {
  const normalized = text.replace(/\s+/g, " ");
  return normalized.length <= maxLength ? normalized : normalized.slice(0, maxLength - 3) + "...";
}

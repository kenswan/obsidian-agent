import { spawn, type ChildProcess } from "node:child_process";
import chalk from "chalk";
import ora from "ora";

export class McpServerManager {
  private serverProcess: ChildProcess | null = null;
  private cliStartedServer = false;

  async ensureRunning(endpoint: string, projectPath: string, verbose: boolean): Promise<boolean> {
    if (await this.isReachable(endpoint)) {
      console.log(chalk.green("MCP server already running."));
      return true;
    }

    console.log(chalk.dim("MCP server not detected. Starting it automatically..."));

    this.serverProcess = this.startProcess(projectPath, verbose);

    if (!this.serverProcess) {
      console.log(chalk.red("Failed to start MCP server."));
      return false;
    }

    this.cliStartedServer = true;

    const healthy = await this.waitForHealthy(endpoint);

    if (!healthy) {
      console.log(chalk.red("Timed out waiting for MCP server to start."));
      await this.stop();
      return false;
    }

    console.log(chalk.green("MCP server is running."));
    return true;
  }

  async dispose(): Promise<void> {
    if (this.cliStartedServer && this.serverProcess) {
      console.log(chalk.dim("Stopping MCP server..."));
      await this.stop();
      console.log(chalk.green("MCP server stopped."));
    }
  }

  private async isReachable(endpoint: string): Promise<boolean> {
    try {
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 3000);
      try {
        await fetch(endpoint, { signal: controller.signal });
        return true;
      } finally {
        clearTimeout(timeout);
      }
    } catch {
      return false;
    }
  }

  private startProcess(projectPath: string, verbose: boolean): ChildProcess | null {
    const args = ["run", "--project", projectPath];
    if (verbose) args.push("--", "--verbose");

    try {
      return spawn("dotnet", args, {
        stdio: verbose ? "inherit" : "ignore",
        detached: false,
      });
    } catch {
      return null;
    }
  }

  private async waitForHealthy(endpoint: string): Promise<boolean> {
    const spinner = ora({ text: "Starting MCP server...", color: "magenta" }).start();
    const deadline = Date.now() + 2 * 60 * 1000;

    try {
      while (Date.now() < deadline) {
        if (await this.isReachable(endpoint)) {
          spinner.succeed("MCP server ready.");
          return true;
        }
        spinner.text = "Waiting for MCP server to become healthy...";
        await new Promise((r) => setTimeout(r, 2000));
      }
      spinner.fail("MCP server did not become healthy in time.");
      return false;
    } catch (err) {
      spinner.fail(`MCP server health check failed: ${(err as Error).message}`);
      return false;
    }
  }

  private async stop(): Promise<void> {
    if (!this.serverProcess) return;

    const proc = this.serverProcess;
    this.serverProcess = null;

    try {
      if (proc.exitCode === null && !proc.killed) {
        proc.kill("SIGTERM");
        await new Promise<void>((resolve) => {
          const timer = setTimeout(() => {
            try {
              proc.kill("SIGKILL");
            } catch {
              // already gone
            }
            resolve();
          }, 3000);
          proc.once("exit", () => {
            clearTimeout(timer);
            resolve();
          });
        });
      }
    } catch {
      // already exited
    }
  }
}

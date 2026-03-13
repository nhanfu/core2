type WorkflowName = "dev" | "start" | "test";

type CommandSpec = {
  name: string;
  cwd: string;
  command: readonly string[];
};

const workflows: Record<WorkflowName, readonly CommandSpec[]> = {
  dev: [
    {
      name: "backend",
      cwd: "svc",
      command: ["deno", "task", "dev"],
    },
    {
      name: "frontend",
      cwd: "frontend",
      command: ["deno", "run", "-A", "npm:vite"],
    },
  ],
  start: [
    {
      name: "backend",
      cwd: "svc",
      command: ["deno", "task", "start"],
    },
    {
      name: "frontend",
      cwd: ".",
      command: ["deno", "run", "-A", "scripts/frontend_start.ts"],
    },
  ],
  test: [
    {
      name: "backend-tests",
      cwd: "svc",
      command: ["deno", "test", "--allow-all"],
    },
    {
      name: "frontend-tests",
      cwd: "frontend",
      command: [
        "node",
        "--experimental-vm-modules",
        "./node_modules/.deno/jest@30.3.0/node_modules/jest/bin/jest.js",
        "lib/test",
        "--runInBand",
        "--config",
        "jest.config.cjs",
      ],
    },
  ],
};

const requiredFiles = [
  "svc/deno.json",
  "frontend/package.json",
] as const;

const requiredBinaries = ["deno"] as const;
const shutdownSignals = Deno.build.os === "windows"
  ? ["SIGINT", "SIGBREAK", "SIGHUP"] as const
  : ["SIGINT", "SIGTERM"] as const;
const processKillSignal = Deno.build.os === "windows" ? "SIGINT" : "SIGTERM";

function fail(message: string): never {
  console.error(message);
  Deno.exit(1);
}

async function ensureFileExists(path: string): Promise<void> {
  try {
    await Deno.stat(path);
  } catch {
    fail(`Missing required file: ${path}`);
  }
}

async function binaryExists(binary: string): Promise<boolean> {
  const command = Deno.build.os === "windows"
    ? new Deno.Command("where.exe", {
      args: [binary],
      stdout: "null",
      stderr: "null",
    })
    : new Deno.Command("sh", {
      args: ["-lc", `command -v ${binary}`],
      stdout: "null",
      stderr: "null",
    });

  const result = await command.output();

  return result.success;
}

async function runPreflightChecks(): Promise<void> {
  for (const path of requiredFiles) {
    await ensureFileExists(path);
  }

  const checks = await Promise.all(
    requiredBinaries.map(async (binary) => ({
      binary,
      exists: await binaryExists(binary),
    })),
  );

  const missing = checks.filter((item) => !item.exists).map((item) => item.binary);
  if (missing.length > 0) {
    fail(`Missing required binaries: ${missing.join(", ")}`);
  }
}

function startProcess(spec: CommandSpec): Deno.ChildProcess {
  console.log(`[${spec.name}] starting: ${spec.command.join(" ")} (${spec.cwd})`);

  return new Deno.Command(spec.command[0], {
    args: spec.command.slice(1),
    cwd: spec.cwd,
    stdin: "inherit",
    stdout: "inherit",
    stderr: "inherit",
  }).spawn();
}

function killProcess(process: Deno.ChildProcess): void {
  try {
    process.kill(processKillSignal);
  } catch {
    // Process may already be closed.
  }
}

async function runConcurrent(specs: readonly CommandSpec[]): Promise<number> {
  const running = specs.map((spec) => ({
    name: spec.name,
    process: startProcess(spec),
  }));

  let shuttingDown = false;

  const shutdown = (code = 0): void => {
    if (shuttingDown) {
      return;
    }

    shuttingDown = true;
    console.log("\nShutting down processes...");

    for (const item of running) {
      killProcess(item.process);
    }

    setTimeout(() => Deno.exit(code), 200);
  };

  for (const signal of shutdownSignals) {
    Deno.addSignalListener(signal, () => shutdown(0));
  }

  const results = await Promise.all(
    running.map(async ({ name, process }) => ({
      name,
      status: await process.status,
    })),
  );

  const failed = results.find((result) => !result.status.success);
  if (failed) {
    console.error(`[${failed.name}] exited with code ${failed.status.code}`);
    return failed.status.code ?? 1;
  }

  return 0;
}

async function runSequential(specs: readonly CommandSpec[]): Promise<number> {
  for (const spec of specs) {
    console.log(`[${spec.name}] running: ${spec.command.join(" ")} (${spec.cwd})`);
    const status = await new Deno.Command(spec.command[0], {
      args: spec.command.slice(1),
      cwd: spec.cwd,
      stdin: "inherit",
      stdout: "inherit",
      stderr: "inherit",
    }).output();

    if (!status.success) {
      console.error(`[${spec.name}] exited with code ${status.code}`);
      return status.code ?? 1;
    }
  }

  return 0;
}

const workflow = (Deno.args[0] ?? "dev") as WorkflowName;

if (!(workflow in workflows)) {
  fail(`Unknown workflow: ${workflow}`);
}

await runPreflightChecks();

const exitCode = workflow === "test"
  ? await runSequential(workflows[workflow])
  : await runConcurrent(workflows[workflow]);

Deno.exit(exitCode);

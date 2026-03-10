const build = new Deno.Command("deno", {
  args: ["run", "-A", "npm:vite", "build"],
  cwd: "frontend",
  stdin: "inherit",
  stdout: "inherit",
  stderr: "inherit",
});

const buildStatus = await build.output();

if (!buildStatus.success) {
  Deno.exit(buildStatus.code ?? 1);
}

const serve = new Deno.Command("deno", {
  args: ["run", "--allow-read", "--allow-net", "scripts/static_server.ts"],
  stdin: "inherit",
  stdout: "inherit",
  stderr: "inherit",
}).spawn();

const serveStatus = await serve.status;
Deno.exit(serveStatus.code ?? 0);

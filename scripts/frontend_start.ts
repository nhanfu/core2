const devServer = new Deno.Command("deno", {
  args: ["run", "-A", "npm:vite"],
  cwd: "frontend",
  stdin: "inherit",
  stdout: "inherit",
  stderr: "inherit",
}).spawn();

const devServerStatus = await devServer.status;
Deno.exit(devServerStatus.code ?? 0);

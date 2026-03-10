import path from "node:path";
import { defineConfig } from "vite";

function restartOnEnvChange() {
  return {
    name: "restart-on-env-change",
    handleHotUpdate({ file, server }) {
      const fileName = path.basename(file);
      if (!fileName.startsWith(".env")) {
        return;
      }

      server.config.logger.info(
        "[vite] .env changed, restarting dev server to reload import.meta.env..."
      );
      server.restart();
      return [];
    },
  };
}

export default defineConfig({
  plugins: [restartOnEnvChange()],
  server: {
    host: true, // Allows the server to be accessed externally
    strictPort: false, // Disables strict host checking,
    allowedHosts: ["logistics.digotech.net", "tms.digotech.net"],
  },
});

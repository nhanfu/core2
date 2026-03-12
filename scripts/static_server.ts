import { serveDir } from "https://deno.land/std@0.224.0/http/file_server.ts";
import { fromFileUrl } from "https://deno.land/std@0.224.0/path/from_file_url.ts";

const DIST_ROOT = fromFileUrl(new URL("../frontend/dist/", import.meta.url));
const port = Number(Deno.env.get("PORT") ?? "4173");

console.log(`Serving frontend/dist on http://0.0.0.0:${port}`);

Deno.serve({ hostname: "0.0.0.0", port }, (request) =>
  serveDir(request, {
    fsRoot: DIST_ROOT,
    urlRoot: "",
    quiet: true,
  })
);

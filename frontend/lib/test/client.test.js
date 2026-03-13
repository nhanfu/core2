import { Client } from "../clients/client.js";

describe("Client", () => {
  let apiMeta;

  beforeEach(() => {
    apiMeta = document.querySelector('meta[name="api"]');
    if (!apiMeta) {
      apiMeta = document.createElement("meta");
      apiMeta.setAttribute("name", "api");
      document.head.appendChild(apiMeta);
    }
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  test("resolveApiBase prefers the runtime api meta tag", () => {
    apiMeta.setAttribute("content", "https://logistics.digotech.net");

    expect(Client.resolveApiBase()).toBe("https://logistics.digotech.net");
  });

  test("buildUrl avoids duplicate slashes between base URL and request path", () => {
    expect(Client.buildUrl("http://localhost:8000/", "/api/auth/login")).toBe(
      "http://localhost:8000/api/auth/login"
    );
  });

  test("submitAsyncWithToken sends requests to the normalized API URL", async () => {
    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: true,
      headers: {
        get: (name) => (name === "Content-Type" ? "application/json" : null),
      },
      json: async () => ({ success: true }),
      text: async () => "",
      blob: async () => new Blob(),
      arrayBuffer: async () => new ArrayBuffer(0),
    });

    const originalApi = Client.api;
    Client.api = "http://localhost:8000/";

    try {
      await Client.instance.submitAsyncWithToken({
        Url: "/api/auth/login",
        Method: "POST",
        jsonData: JSON.stringify({ userName: "demo", password: "demo" }),
        allowAnonymous: true,
      });
    } finally {
      Client.api = originalApi;
    }

    expect(globalThis.fetch).toHaveBeenCalledWith(
      "http://localhost:8000/api/auth/login",
      expect.objectContaining({
        method: "POST",
      })
    );
  });
});

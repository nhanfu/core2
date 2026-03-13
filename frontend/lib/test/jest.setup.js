import { TextDecoder, TextEncoder } from "node:util";
import { jest } from "@jest/globals";

globalThis.jest = jest;

if (!globalThis.TextEncoder) {
  globalThis.TextEncoder = TextEncoder;
}

if (!globalThis.TextDecoder) {
  globalThis.TextDecoder = TextDecoder;
}

if (!globalThis.window.matchMedia) {
  globalThis.window.matchMedia = () => ({
    matches: false,
    media: "",
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}

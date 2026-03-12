import { textDecoder, textEncoder } from "node:util";
import { jest } from "@jest/globals";

globalThis.jest = jest;

if (!globalThis.textEncoder) {
  globalThis.textEncoder = textEncoder;
}

if (!globalThis.textDecoder) {
  globalThis.textDecoder = textDecoder;
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

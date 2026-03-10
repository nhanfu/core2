module.exports = {
  roots: ["<rootDir>/lib/test"],
  testEnvironment: "jsdom",
  modulePathIgnorePatterns: [
    "<rootDir>/dist/",
    "<rootDir>/public/",
  ],
  moduleNameMapper: {
    "\\.(css|less|scss|sass)$": "<rootDir>/lib/test/styleMock.js",
    "^(\\.{1,2}/)+index(\\.js)?$": "<rootDir>/lib/test/indexMock.js",
    "^emoji-mart$": "<rootDir>/lib/test/emojiMartMock.js",
    "^tinymce$": "<rootDir>/lib/test/tinymceMock.js",
    "^sortablejs$": "<rootDir>/lib/test/sortableMock.js",
  },
  setupFiles: ["<rootDir>/lib/test/jest.setup.js"],
  transform: {},
};

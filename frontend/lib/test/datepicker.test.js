import { jest } from "@jest/globals";
import dayjs from "dayjs";

globalThis.hTMLInputElement = HTMLInputElement;

jest.unstable_mockModule("../utils/componentExt.js", () => ({
  ComponentExt: {},
}));

jest.unstable_mockModule("flatpickr", () => ({
  default: jest.fn((input) => ({
    input,
    isOpen: false,
    setDate: jest.fn(),
    open: jest.fn(),
    close: jest.fn(),
    destroy: jest.fn(),
    _positionCalendar: jest.fn(),
  })),
}));

jest.unstable_mockModule("flatpickr/dist/l10n/vn.js", () => ({
  vietnamese: {},
}));

const { Datepicker } = await import("../datepicker.js");

describe("Datepicker", () => {
  let datepicker;
  let container;
  let meta;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    meta = {
      fieldName: "startDate",
      plainText: "Pick a date",
      formatData: "",
      precision: 0,
      showHotKey: true,
    };
    datepicker = new Datepicker(meta, container);
    datepicker.entity = {};
    datepicker.render();
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  test("renders an input and initializes flatpickr", () => {
    expect(datepicker.input).toBeInstanceOf(hTMLInputElement);
    expect(datepicker.flatpickr).toBeDefined();
  });

  test("Value setter writes formatted date data into the input and entity", () => {
    const testDate = dayjs("2023-04-26");

    datepicker.value = testDate;

    expect(datepicker.value).toBe(testDate);
    expect(datepicker.input.value).toBe("26/04/2023");
    expect(datepicker.entity.startDate).toBe("2023-04-26T00:00:00");
  });

  test("setting a null value clears the input and entity value", () => {
    datepicker.value = dayjs("2023-04-26");
    datepicker.value = null;

    expect(datepicker.input.value).toBe("");
    expect(datepicker.entity.startDate).toBeNull();
  });

  test("disabled state updates the input element", () => {
    datepicker.disabled = true;

    expect(datepicker.input.disabled).toBe(true);
  });
});

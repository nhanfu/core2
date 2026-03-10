import { jest } from "@jest/globals";
import dayjs from "dayjs";

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
  Vietnamese: {},
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
      FieldName: "StartDate",
      PlainText: "Pick a date",
      FormatData: "",
      Precision: 0,
      ShowHotKey: true,
    };
    datepicker = new Datepicker(meta, container);
    datepicker.Entity = {};
    datepicker.Render();
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  test("renders an input and initializes flatpickr", () => {
    expect(datepicker.Input).toBeInstanceOf(HTMLInputElement);
    expect(datepicker.flatpickr).toBeDefined();
  });

  test("Value setter writes formatted date data into the input and entity", () => {
    const testDate = dayjs("2023-04-26");

    datepicker.Value = testDate;

    expect(datepicker.Value).toBe(testDate);
    expect(datepicker.Input.value).toBe("26/04/2023");
    expect(datepicker.Entity.StartDate).toBe("2023-04-26T00:00:00");
  });

  test("setting a null value clears the input and entity value", () => {
    datepicker.Value = dayjs("2023-04-26");
    datepicker.Value = null;

    expect(datepicker.Input.value).toBe("");
    expect(datepicker.Entity.StartDate).toBeNull();
  });

  test("disabled state updates the input element", () => {
    datepicker.Disabled = true;

    expect(datepicker.Input.disabled).toBe(true);
  });
});

import Decimal from "decimal.js";
import { numBox } from "../numbox";

describe("Numbox", () => {
  let numbox;
  let input;

  beforeEach(() => {
    input = document.createElement("input");
    numbox = new numBox({ fieldName: "Amount", Precision: 2 }, input);
    numbox.Entity = {};
    numbox.populateFields = jest.fn();
    numbox.dispatchEvent = jest.fn().mockResolvedValue(true);
    numbox.render();
  });

  test("Value setter formats numeric values into the input", () => {
    numbox.Value = 123.456;

    expect(numbox.Value.toString()).toBe("123.456");
    expect(input.value).toBe("123.46");
  });

  test("Value setter clears the input for null values", () => {
    numbox.Value = null;

    expect(numbox.Value).toBeNull();
    expect(input.value).toBe("");
  });

  test("input handler parses user input without formatting loss", () => {
    input.value = "1234.56";
    input.dispatchEvent(new Event("input"));

    expect(numbox.Value.toString()).toBe("1234.56");
    expect(numbox.Entity.Amount.toString()).toBe("1234.56");
  });

  test("change handler marks the component dirty and populates fields", () => {
    input.value = "789.01";
    input.dispatchEvent(new Event("change"));

    expect(numbox.Value.toString()).toBe("789.01");
    expect(numbox.Dirty).toBe(true);
    expect(numbox.populateFields).toHaveBeenCalled();
  });

  test("invalid input restores the previous numeric value", () => {
    numbox.Value = new Decimal(500);
    input.value = "invalid text";

    input.dispatchEvent(new Event("input"));

    expect(numbox.Value.toString()).toBe("500");
    expect(input.value).toBe("500.00");
  });

  test("Value setter respects precision and thousands separators", () => {
    numbox.Value = 1234.5;

    expect(input.value).toBe("1,234.50");
  });
});

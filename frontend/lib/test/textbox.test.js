import { Textbox } from "../textbox";
import { Utils } from "../utils/utils.js";
import { Client } from "../clients/client.js";
import { ValidationRule } from "../models/index.js";

describe("Textbox", () => {
  let textbox;
  let element;
  let meta;
  let entity;

  beforeEach(() => {
    element = document.createElement("input");
    meta = {
      Id: "txt-test",
      FieldName: "testField",
      Label: "Test Field",
      PlainText: "Enter text",
      Events: [],
      ShowLabel: true,
    };

    textbox = new Textbox(meta, element);
    entity = { testField: "Initial Value" };
    textbox.Entity = entity;
    textbox.EditForm = { Meta: { IgnoreEncode: false, EntityName: "TestEntity" } };
    textbox.Render();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  test("constructor and render initialize from the entity", () => {
    expect(textbox.Meta).toBe(meta);
    expect(textbox.Input).toBe(element);
    expect(textbox.Text).toBe("Initial Value");
    expect(textbox.Value).toBe("Initial Value");
  });

  test("Text setter updates the input element", () => {
    textbox.Text = "New Text";

    expect(textbox.Text).toBe("New Text");
    expect(textbox.Input.value).toBe("New Text");
  });

  test("Value setter updates component text and entity state", () => {
    textbox.Value = "New Value";

    expect(textbox.Value).toBe("New Value");
    expect(textbox.Text).toBe("New Value");
    expect(textbox.Input.value).toBe("New Value");
    expect(textbox.Entity.testField).toBe("New Value");
  });

  test("ValidateUnique clears unique validation errors when query is empty", async () => {
    textbox.ValidationRules = {
      [ValidationRule.Unique]: { Message: "Must be unique" },
    };
    textbox.ValidationResult = {
      [ValidationRule.Unique]: "old error",
    };
    textbox._text = "Candidate";
    jest.spyOn(Utils, "IsFunction").mockReturnValue(null);
    Client.Instance.ComQuery = jest.fn().mockResolvedValue([]);

    const result = await textbox.ValidateUnique();

    expect(result).toBe(true);
    expect(Client.Instance.ComQuery).toHaveBeenCalled();
    expect(textbox.ValidationResult[ValidationRule.Unique]).toBeUndefined();
  });

  test("SetDisableUI toggles readonly state", () => {
    textbox.SetDisableUI(true);
    expect(textbox.Input.readOnly).toBe(true);

    textbox.SetDisableUI(false);
    expect(textbox.Input.readOnly).toBe(false);
  });

  test("PopulateUIChange updates state from user input", () => {
    textbox.Input.value = "Updated Text";

    textbox.PopulateUIChange("input");

    expect(textbox.Text).toBe("Updated Text");
    expect(textbox.Value).toBe("Updated Text");
    expect(textbox.Entity.testField).toBe("Updated Text");
    expect(textbox.Dirty).toBe(true);
  });
});

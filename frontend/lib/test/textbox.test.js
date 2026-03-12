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
      fieldName: "testField",
      Label: "Test Field",
      plainText: "Enter text",
      Events: [],
      showLabel: true,
    };

    textbox = new Textbox(meta, element);
    entity = { testField: "Initial Value" };
    textbox.Entity = entity;
    textbox.editForm = { Meta: { ignoreEncode: false, entityName: "testEntity" } };
    textbox.render();
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

  test("validateUnique clears unique validation errors when query is empty", async () => {
    textbox.validationRules = {
      [ValidationRule.Unique]: { Message: "Must be unique" },
    };
    textbox.validationResult = {
      [ValidationRule.Unique]: "old error",
    };
    textbox._text = "Candidate";
    jest.spyOn(Utils, "isFunction").mockReturnValue(null);
    Client.Instance.comQuery = jest.fn().mockResolvedValue([]);

    const result = await textbox.validateUnique();

    expect(result).toBe(true);
    expect(Client.Instance.comQuery).toHaveBeenCalled();
    expect(textbox.validationResult[ValidationRule.Unique]).toBeUndefined();
  });

  test("setDisableUI toggles readonly state", () => {
    textbox.setDisableUI(true);
    expect(textbox.Input.readOnly).toBe(true);

    textbox.setDisableUI(false);
    expect(textbox.Input.readOnly).toBe(false);
  });

  test("populateUIChange updates state from user input", () => {
    textbox.Input.value = "Updated Text";

    textbox.populateUIChange("input");

    expect(textbox.Text).toBe("Updated Text");
    expect(textbox.Value).toBe("Updated Text");
    expect(textbox.Entity.testField).toBe("Updated Text");
    expect(textbox.Dirty).toBe(true);
  });
});

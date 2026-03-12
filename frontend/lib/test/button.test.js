import { Button } from "../button";

describe("Button", () => {
  let ui;
  let element;
  let button;

  beforeEach(() => {
    ui = {
      Id: "123",
      fieldName: "btnSave",
      className: "btn-class",
      Style: "color: red;",
      Icon: "icon-path",
      Label: "Click me",
      Events: "{}",
    };
    element = document.createElement("button");
    document.body.appendChild(element);
    button = new Button(ui, element);
    button.editForm = { Meta: { Label: "Form label" } };
    button.Entity = {};
  });

  afterEach(() => {
    document.body.removeChild(element);
    jest.restoreAllMocks();
  });

  test("Render applies the configured markup and styles", () => {
    button.render();

    expect(button.element).toBe(element);
    expect(button.element.className).toContain("btn-class");
    expect(button.element.style.color).toBe("red");
    expect(button.element.querySelector(".caption").textContent).toBe("Click me");
  });

  test("dispatchClick delegates to Meta.onClick when present", () => {
    const onClick = jest.fn();
    button.Meta.onClick = onClick;

    button.dispatchClick();

    expect(onClick).toHaveBeenCalled();
  });

  test("dispatchClick calls dispatchEvent for enabled buttons", async () => {
    button.render();
    const dispatchEvent = jest.fn().mockResolvedValue(true);
    button.dispatchEvent = dispatchEvent;

    button.dispatchClick();

    expect(dispatchEvent).toHaveBeenCalledWith(button.Meta.Events, "click", button, button.Entity);
    expect(button.Disabled).toBe(true);
  });

  test("dispatchClick stops when the button is disabled", () => {
    button.render();
    const dispatchEvent = jest.fn();
    button.dispatchEvent = dispatchEvent;
    button.Disabled = true;

    button.dispatchClick();

    expect(dispatchEvent).not.toHaveBeenCalled();
  });

  test("getValueText returns the entity field value when available", () => {
    button.render();
    button.Entity = { btnSave: "Some text" };

    expect(button.getValueText()).toBe("Some text");
  });
});

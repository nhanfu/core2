import { Button } from "../button";

describe("Button", () => {
  let ui;
  let element;
  let button;

  beforeEach(() => {
    ui = {
      Id: "123",
      FieldName: "btnSave",
      ClassName: "btn-class",
      Style: "color: red;",
      Icon: "icon-path",
      Label: "Click me",
      Events: "{}",
    };
    element = document.createElement("button");
    document.body.appendChild(element);
    button = new Button(ui, element);
    button.EditForm = { Meta: { Label: "Form label" } };
    button.Entity = {};
  });

  afterEach(() => {
    document.body.removeChild(element);
    jest.restoreAllMocks();
  });

  test("Render applies the configured markup and styles", () => {
    button.Render();

    expect(button.Element).toBe(element);
    expect(button.Element.className).toContain("btn-class");
    expect(button.Element.style.color).toBe("red");
    expect(button.Element.querySelector(".caption").textContent).toBe("Click me");
  });

  test("DispatchClick delegates to Meta.OnClick when present", () => {
    const onClick = jest.fn();
    button.Meta.OnClick = onClick;

    button.DispatchClick();

    expect(onClick).toHaveBeenCalled();
  });

  test("DispatchClick calls DispatchEvent for enabled buttons", async () => {
    button.Render();
    const dispatchEvent = jest.fn().mockResolvedValue(true);
    button.DispatchEvent = dispatchEvent;

    button.DispatchClick();

    expect(dispatchEvent).toHaveBeenCalledWith(button.Meta.Events, "click", button, button.Entity);
    expect(button.Disabled).toBe(true);
  });

  test("DispatchClick stops when the button is disabled", () => {
    button.Render();
    const dispatchEvent = jest.fn();
    button.DispatchEvent = dispatchEvent;
    button.Disabled = true;

    button.DispatchClick();

    expect(dispatchEvent).not.toHaveBeenCalled();
  });

  test("GetValueText returns the entity field value when available", () => {
    button.Render();
    button.Entity = { btnSave: "Some text" };

    expect(button.GetValueText()).toBe("Some text");
  });
});

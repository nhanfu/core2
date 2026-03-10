import { jest } from "@jest/globals";

jest.unstable_mockModule("../utils/componentFactory.js", () => ({
  ComponentFactory: {},
}));

jest.unstable_mockModule("../clients/client.js", () => ({
  Client: { Instance: {} },
}));

jest.unstable_mockModule("../tabComponent.js", () => ({
  TabComponent: class TabComponent {},
}));

jest.unstable_mockModule("../tabGroup.js", () => ({
  TabGroup: class TabGroup {},
}));

const { Section } = await import("../section.js");

describe("Section", () => {
  let section;
  let container;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    section = new Section("div");
    section.ParentElement = container;
    section.Element = container;
    section.Entity = {};
    section.EditForm = {};
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  test("constructor initializes defaults", () => {
    expect(section.Children).toEqual([]);
    expect(section.Element).toBe(container);
  });

  test("HasElementAndAll requires a non-empty array and a passing predicate", () => {
    expect(Section.HasElementAndAll([], () => true)).toBe(false);
    expect(Section.HasElementAndAll([1, 2, 3], value => value > 0)).toBe(true);
    expect(Section.HasElementAndAll([1, 0, 3], value => value > 0)).toBe(false);
  });

  test("HandleMeta injects HTML and scoped CSS", () => {
    section.Meta = {
      Id: "abc",
      FieldName: "TestField",
      Html: "<div class='child'>Hello</div>",
      Css: ".child { color: red; }",
    };

    section.HandleMeta();

    expect(section.Element.innerHTML).toContain("Hello");
    expect(document.head.querySelector("#testfieldabc-style")).not.toBeNull();
  });

  test("DropdownBtnClick toggles dropdown visibility", () => {
    section.Meta = { Label: "Actions" };
    section.RenderDropDown();

    expect(section.innerEle.style.display).toBe("none");

    section.DropdownBtnClick();
    expect(section.innerEle.style.display).toBe("block");

    section.DropdownBtnClick();
    expect(section.innerEle.style.display).toBe("none");
  });
});

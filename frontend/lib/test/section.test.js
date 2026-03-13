import { jest } from "@jest/globals";

jest.unstable_mockModule("../clients/client.js", () => ({
  Client: { Instance: {} },
}));

jest.unstable_mockModule("../tabComponent.js", () => ({
  tabComponent: class TabComponent {},
}));

jest.unstable_mockModule("../tabGroup.js", () => ({
  tabGroup: class TabGroup {},
}));

const { Section } = await import("../section.js");

describe("Section", () => {
  let section;
  let container;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    section = new Section("div");
    section.parentElement = container;
    section.element = container;
    section.Entity = {};
    section.editForm = {};
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  test("constructor initializes defaults", () => {
    expect(section.Children).toEqual([]);
    expect(section.element).toBe(container);
  });

  test("hasElementAndAll requires a non-empty array and a passing predicate", () => {
    expect(Section.hasElementAndAll([], () => true)).toBe(false);
    expect(Section.hasElementAndAll([1, 2, 3], value => value > 0)).toBe(true);
    expect(Section.hasElementAndAll([1, 0, 3], value => value > 0)).toBe(false);
  });

  test("handleMeta injects HTML and scoped CSS", () => {
    section.Meta = {
      Id: "abc",
      fieldName: "testField",
      Html: "<div class='child'>Hello</div>",
      Css: ".child { color: red; }",
    };

    section.handleMeta();

    expect(section.element.innerHTML).toContain("Hello");
    expect(document.head.querySelector("#testfieldabc-style")).not.toBeNull();
  });

  test("dropdownBtnClick toggles dropdown visibility", () => {
    section.Meta = { Label: "Actions" };
    section.renderDropDown();

    expect(section.innerEle.style.display).toBe("none");

    section.dropdownBtnClick();
    expect(section.innerEle.style.display).toBe("block");

    section.dropdownBtnClick();
    expect(section.innerEle.style.display).toBe("none");
  });
});

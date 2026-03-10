import { jest } from "@jest/globals";

const renderSection = jest.fn();

jest.unstable_mockModule("../section.js", () => ({
  Section: {
    RenderSection: renderSection,
    RenderGroupContent: jest.fn(),
  },
}));

const { TabComponent } = await import("../tabComponent.js");
const { TabGroup } = await import("../tabGroup.js");

describe("TabGroup", () => {
  let tabGroup;

  beforeEach(() => {
    tabGroup = new TabGroup();
    tabGroup.Meta = { IsVertialTab: false, Children: [] };
    tabGroup.ParentElement = document.createElement("div");
    tabGroup.EditForm = { ButtonFrozen: null, IsLoadButtonFrozen: false };
  });

  test("initializes default values", () => {
    expect(tabGroup.ListViewType).toEqual(["ListView", "GroupListView", "GridView", "GroupGridView"]);
    expect(tabGroup.Ul).toBeNull();
    expect(tabGroup.TabContent).toBeNull();
    expect(tabGroup.ShouldCountBage).toBe(false);
    expect(tabGroup.HasRendered).toBe(false);
  });

  test("renders the tab group scaffold", () => {
    tabGroup.Render();

    expect(tabGroup.Ul).toBeInstanceOf(HTMLUListElement);
    expect(tabGroup.Element).toBeInstanceOf(HTMLDivElement);
    expect(tabGroup.TabContent).toBeInstanceOf(HTMLDivElement);
  });
});

describe("TabComponent", () => {
  let tabComponent;
  let mockGroup;

  beforeEach(() => {
    mockGroup = {
      FieldName: "TestTab",
      Id: "test-id",
      Icon: "test-icon",
      Label: "Test Label",
      Description: "Test Description",
      Events: [],
      Components: [],
    };
    tabComponent = new TabComponent(mockGroup);
    tabComponent.Parent = new TabGroup();
    tabComponent.Parent.Ul = document.createElement("ul");
    tabComponent.Parent.TabContent = document.createElement("div");
    tabComponent.Parent.Children = [tabComponent];
    tabComponent.EditForm = {
      Meta: { Label: "Form label" },
      TabComponents: [],
      ResizeListView: jest.fn(),
    };
  });

  afterEach(() => {
    renderSection.mockReset();
  });

  test("initializes default values", () => {
    expect(tabComponent.Meta).toEqual(mockGroup);
    expect(tabComponent.Name).toBe(mockGroup.FieldName);
    expect(tabComponent._li).toBeUndefined();
    expect(tabComponent.BadgeElement).toBeUndefined();
  });

  test("renders the tab header", () => {
    tabComponent.Render();

    expect(tabComponent._li).toBeInstanceOf(HTMLLIElement);
    expect(tabComponent.TextElement.textContent).toContain("Test Label");
  });

  test("Focus toggles active state", () => {
    tabComponent.Render();
    tabComponent.Focus();

    expect(tabComponent._li.classList.contains("active")).toBe(true);
  });

  test("RenderTabContent delegates to Section.RenderSection", () => {
    tabComponent.Render();
    tabComponent.RenderTabContent();

    expect(renderSection).toHaveBeenCalledWith(tabComponent, mockGroup, null, tabComponent.EditForm);
    expect(tabComponent.HasRendered).toBe(true);
  });
});

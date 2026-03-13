import { jest } from "@jest/globals";

const renderSection = jest.fn();

jest.unstable_mockModule("../section.js", () => ({
  Section: {
    renderSection: renderSection,
    renderGroupContent: jest.fn(),
  },
}));

const { TabComponent } = await import("../tabComponent.js");
const { TabGroup } = await import("../tabGroup.js");

describe("TabGroup", () => {
  let tabGroup;

  beforeEach(() => {
    tabGroup = new TabGroup();
    tabGroup.Meta = { isVertialTab: false, Children: [] };
    tabGroup.parentElement = document.createElement("div");
    tabGroup.editForm = { buttonFrozen: null, isLoadButtonFrozen: false };
  });

  test("initializes default values", () => {
    expect(tabGroup.listViewType).toEqual(["ListView", "GroupListView", "GridView", "GroupGridView"]);
    expect(tabGroup.ul).toBeNull();
    expect(tabGroup.tabContent).toBeNull();
    expect(tabGroup.shouldCountBage).toBe(false);
    expect(tabGroup.hasRendered).toBe(false);
  });

  test("renders the tab group scaffold", () => {
    tabGroup.render();

    expect(tabGroup.ul).toBeInstanceOf(hTMLUListElement);
    expect(tabGroup.element).toBeInstanceOf(hTMLDivElement);
    expect(tabGroup.tabContent).toBeInstanceOf(hTMLDivElement);
  });
});

describe("TabComponent", () => {
  let tabComponent;
  let mockGroup;

  beforeEach(() => {
    mockGroup = {
      fieldName: "testTab",
      Id: "test-id",
      Icon: "test-icon",
      Label: "Test Label",
      Description: "Test Description",
      Events: [],
      Components: [],
    };
    tabComponent = new TabComponent(mockGroup);
    tabComponent.Parent = new TabGroup();
    tabComponent.Parent.ul = document.createElement("ul");
    tabComponent.Parent.tabContent = document.createElement("div");
    tabComponent.Parent.Children = [tabComponent];
    tabComponent.editForm = {
      Meta: { Label: "Form label" },
      tabComponents: [],
      resizeListView: jest.fn(),
    };
  });

  afterEach(() => {
    renderSection.mockReset();
  });

  test("initializes default values", () => {
    expect(tabComponent.Meta).toEqual(mockGroup);
    expect(tabComponent.Name).toBe(mockGroup.fieldName);
    expect(tabComponent._li).toBeUndefined();
    expect(tabComponent.badgeElement).toBeUndefined();
  });

  test("renders the tab header", () => {
    tabComponent.render();

    expect(tabComponent._li).toBeInstanceOf(hTMLLIElement);
    expect(tabComponent.textElement.textContent).toContain("Test Label");
  });

  test("Focus toggles active state", () => {
    tabComponent.render();
    tabComponent.Focus();

    expect(tabComponent._li.classList.contains("active")).toBe(true);
  });

  test("renderTabContent delegates to Section.renderSection", () => {
    tabComponent.render();
    tabComponent.renderTabContent();

    expect(renderSection).toHaveBeenCalledWith(tabComponent, mockGroup, null, tabComponent.editForm);
    expect(tabComponent.hasRendered).toBe(true);
  });
});

import { jest } from "@jest/globals";

class MockListView {
  constructor(ui) {
    this.Meta = ui;
    this.Entity = {};
    this.Header = [];
    this.Editable = ui.CanWrite;
    this.DOMContentLoaded = { add: jest.fn() };
  }

  PopulateFields() {
    if (!this.Meta.PopulateField || !this.EditForm?.UpdateView) {
      return;
    }
    this.EditForm.UpdateView(true, this.Meta.PopulateField.split(","));
  }
}

class MockListViewSection {
  constructor(element = document.createElement("div")) {
    this.Element = element;
  }
}

jest.unstable_mockModule("../listView.js", () => ({
  ListView: MockListView,
}));

jest.unstable_mockModule("../listViewSection.js", () => ({
  ListViewSection: MockListViewSection,
}));

jest.unstable_mockModule("../listViewItem.js", () => ({
  ListViewItem: class ListViewItem {},
}));

jest.unstable_mockModule("../groupGridView.js", () => ({
  GroupGridView: class GroupGridView {},
}));

jest.unstable_mockModule("../multipleSearchEntry.js", () => ({
  MultipleSearchEntry: class MultipleSearchEntry {},
}));

const { GridView } = await import("../gridView.js");
const { Html } = await import("../utils/html.js");

describe("GridView", () => {
  let gridView;
  let container;
  let meta;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);

    meta = {
      Id: "testGridView",
      IsSumary: true,
      CanWrite: true,
      PopulateField: "",
      Label: "Test GridView",
    };

    gridView = new GridView(meta);
    gridView.ParentElement = container;
    gridView.EditForm = {
      UpdateView: jest.fn(),
      Meta: { FeaturePolicy: [] },
      ResizeListView: jest.fn(),
    };
    gridView.headerSection = new MockListViewSection(document.createElement("div"));
    gridView.searchSection = new MockListViewSection(document.createElement("div"));
    gridView.mainSection = new MockListViewSection(document.createElement("div"));
    gridView.emptySection = new MockListViewSection(document.createElement("div"));
    gridView.footerSection = new MockListViewSection(document.createElement("div"));
  });

  afterEach(() => {
    document.body.innerHTML = "";
    jest.restoreAllMocks();
  });

  test("constructor initializes expected defaults", () => {
    expect(gridView.Meta).toEqual(meta);
    expect(gridView.summaryClass).toBe("summary");
    expect(gridView.cellCountNoSticky).toBe(50);
    expect(gridView._summarys).toEqual([]);
  });

  test("DOMContentLoadedHandler calls AddSummaries when summary is enabled", () => {
    gridView.AddSummaries = jest.fn();

    gridView.DOMContentLoadedHandler();

    expect(gridView.AddSummaries).toHaveBeenCalled();
  });

  test("PopulateFields forwards requested fields to the edit form", () => {
    gridView.Meta.PopulateField = "field1,field2";

    gridView.PopulateFields();

    expect(gridView.EditForm.UpdateView).toHaveBeenCalledWith(true, ["field1", "field2"]);
  });

  test("Rerender refreshes visible headers and content", () => {
    gridView.Header = [{ Hidden: false }, { Hidden: true }];
    gridView.RenderTableHeader = jest.fn();
    gridView.AddNewEmptyRow = jest.fn();
    gridView.RenderContent = jest.fn();
    gridView.UpdateStickyColumns = jest.fn();

    gridView.Rerender();

    expect(gridView.loadRerender).toBe(true);
    expect(gridView.RenderTableHeader).toHaveBeenCalledWith([{ Hidden: false }]);
    expect(gridView.AddNewEmptyRow).toHaveBeenCalled();
    expect(gridView.RenderContent).toHaveBeenCalled();
    expect(gridView.UpdateStickyColumns).toHaveBeenCalled();
  });

  test("UpdateStickyColumns applies sticky classes to frozen columns", () => {
    gridView.Header = [{ Frozen: true }, { Frozen: false }];
    gridView.dataTable = document.createElement("table");
    gridView.dataTable.innerHTML = "<tr><th></th><td></td></tr>";
    gridView.headerSection.Element.innerHTML = "<table><tr><th>Frozen</th><th>Free</th></tr></table>";
    gridView.searchSection.Element.innerHTML = "<table><tr><td>A</td><td>B</td></tr></table>";
    gridView.mainSection.Element.innerHTML = "<table><tr><td>C</td><td>D</td></tr></table>";
    gridView.emptySection.Element.innerHTML = "<table><tr><td>E</td><td>F</td></tr></table>";
    gridView.footerSection.Element.innerHTML = "<table><tr><td>G</td><td>H</td></tr></table>";

    gridView.UpdateStickyColumns();

    expect(gridView.headerSection.Element.querySelector("th").classList.contains("sticky-column")).toBe(true);
    expect(gridView.mainSection.Element.querySelector("td").classList.contains("sticky-column")).toBe(true);
  });
});

import { jest } from "@jest/globals";

class MockListView {
  constructor(ui) {
    this.meta = ui;
    this.Entity = {};
    this.Header = [];
    this.Editable = ui.canWrite;
    this.dOMContentLoaded = { add: jest.fn() };
  }

  populateFields() {
    if (!this.meta.populateField || !this.editForm?.updateView) {
      return;
    }
    this.editForm.updateView(true, this.meta.populateField.split(","));
  }
}

class MockListViewSection {
  constructor(element = document.createElement("div")) {
    this.element = element;
  }
}

jest.unstable_mockModule("../listView.js", () => ({
  listView: MockListView,
}));

jest.unstable_mockModule("../listViewSection.js", () => ({
  listViewSection: MockListViewSection,
}));

jest.unstable_mockModule("../listViewItem.js", () => ({
  listViewItem: class ListViewItem {},
}));

jest.unstable_mockModule("../groupGridView.js", () => ({
  groupGridView: class GroupGridView {},
}));

jest.unstable_mockModule("../multipleSearchEntry.js", () => ({
  multipleSearchEntry: class MultipleSearchEntry {},
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
      isSumary: true,
      canWrite: true,
      populateField: "",
      Label: "Test GridView",
    };

    gridView = new GridView(meta);
    gridView.parentElement = container;
    gridView.editForm = {
      updateView: jest.fn(),
      Meta: { featurePolicy: [] },
      resizeListView: jest.fn(),
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

  test("dOMContentLoadedHandler calls addSummaries when summary is enabled", () => {
    gridView.addSummaries = jest.fn();

    gridView.dOMContentLoadedHandler();

    expect(gridView.addSummaries).toHaveBeenCalled();
  });

  test("populateFields forwards requested fields to the edit form", () => {
    gridView.Meta.populateField = "field1,field2";

    gridView.populateFields();

    expect(gridView.editForm.updateView).toHaveBeenCalledWith(true, ["field1", "field2"]);
  });

  test("Rerender refreshes visible headers and content", () => {
    gridView.Header = [{ Hidden: false }, { Hidden: true }];
    gridView.renderTableHeader = jest.fn();
    gridView.addNewEmptyRow = jest.fn();
    gridView.renderContent = jest.fn();
    gridView.updateStickyColumns = jest.fn();

    gridView.Rerender();

    expect(gridView.loadRerender).toBe(true);
    expect(gridView.renderTableHeader).toHaveBeenCalledWith([{ Hidden: false }]);
    expect(gridView.addNewEmptyRow).toHaveBeenCalled();
    expect(gridView.renderContent).toHaveBeenCalled();
    expect(gridView.updateStickyColumns).toHaveBeenCalled();
  });

  test("updateStickyColumns applies sticky classes to frozen columns", () => {
    gridView.Header = [{ Frozen: true }, { Frozen: false }];
    gridView.dataTable = document.createElement("table");
    gridView.dataTable.innerHTML = "<tr><th></th><td></td></tr>";
    gridView.headerSection.element.innerHTML = "<table><tr><th>Frozen</th><th>Free</th></tr></table>";
    gridView.searchSection.element.innerHTML = "<table><tr><td>A</td><td>B</td></tr></table>";
    gridView.mainSection.element.innerHTML = "<table><tr><td>C</td><td>D</td></tr></table>";
    gridView.emptySection.element.innerHTML = "<table><tr><td>E</td><td>F</td></tr></table>";
    gridView.footerSection.element.innerHTML = "<table><tr><td>G</td><td>H</td></tr></table>";

    gridView.updateStickyColumns();

    expect(gridView.headerSection.element.querySelector("th").classList.contains("sticky-column")).toBe(true);
    expect(gridView.mainSection.element.querySelector("td").classList.contains("sticky-column")).toBe(true);
  });
});

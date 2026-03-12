import { EditableComponent } from "./editableComponent.js";
import {
    ObservableArgs, EventType, SqlViewModel, PatchVM, FeaturePolicy,
    customEventType, Component, Action, ElementType, EntityRef
} from "./models/";
import { activeStateEnum, AdvSearchVM, CellSelected, operatorEnum, OrderBy, Where } from "./models/enum.js";
import { Paginator } from "./paginator.js";
import { Utils } from "./utils/utils.js";
import { ObservableList } from './models/observableList.js';
import { ListViewSection } from "./listViewSection.js";
import { Html } from "./utils/html.js";
import { ContextMenu } from "./contextMenu.js";
import { Client } from "./clients/client.js";
import { ListViewSearch } from "./listViewSearch.js";
import { ListViewItem } from "./listViewItem.js";
import { Toast } from "./toast.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { ConfirmDialog } from "./confirmDialog.js";
import { ComponentExt, LangSelect } from "./index.js";
import { Datepicker } from "./index.js";
import * as XLSX from 'xlsx';
import { Select } from "./select.js";
import { Label } from "./label.js";
import { Spinner } from "./spinner.js";
/**
 * Represents a list view component that allows editable features and other interactions like sorting and pagination.
 * @typedef {import('./searchEntry.js').searchEntry} SearchEntry
 * @typedef {import('./tabEditor.js').tabEditor} TabEditor
 * @typedef {import('./gridView.js').gridView} GridView
 */
export class ListView extends EditableComponent {
    selectedIds = [];
    /** @type {ListViewSection} */
    mainSection;
    /**
     * @type {OrderBy[]}
     */
    OrderBy = [];
    /**
     * @type {any[]}
     */
    cacheData = [];
    /**
     * @type {any[]}
     */
    refData = [];
    dataLoaded = new Action();
    dblClick = new Action();
    rowClick = new Action();
    _groupKey = "__groupkey__";
    groupRowClass = "group-row";
    /** @type {string} */
    focusId;
    get Editable() { return this.meta.canWrite; }
    /**
     * Constructs an instance of ListView with the specified UI component.
     * @param {Component} ui The UI component associated with this list view.
     * @param {HTMLElement} [ele] Optional HTML element.
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.isListView = true;
        this.deleteTempIds = [];
        this.meta = ui;
        this.Id = ui.Id;
        this.Name = ui.fieldName;
        /** @type {Component[]} */
        this.Header = [];
        this.rowData = new ObservableList();
        /** @type {AdvSearchVM} */
        // @ts-ignore
        this.advSearchVM = {
            activeState: activeStateEnum.yes,
            Conditions: [],
            advSearchConditions: [],
            orderBy: localStorage.getItem('OrderBy' + this.meta.Id) ?? []
        };
        this._hasLoadRef = false;
        if (ele !== null) {
            this.Resolve(ui, ele);
        }
        this.canDelete = true;
        this._rowHeight = this.meta.bodyItemHeight ?? 26;
        this._theadTable = this.meta.headerHeight ?? 40;
        this._tfooterTable = this.meta.footerHeight ?? 35;
        this._scrollTable = this.meta.scrollHeight ?? 10;
        this._preQueryFn = Utils.isFunction(this.meta.preQuery, false, this);
        /** @type {ListViewItem} */
        this.lastShiftViewItem = undefined;
        /** @type {number} */
        this.lastIndex = undefined;
        this.entityFocusId = "";
        /** @type {HTMLElement} */
        this.lastElementFocus = null;
        /** @type {Component} */
        this.lastComponentFocus = null;
        this.toolbarColumn = {
            statusBar: true,
            Label: '',
            Frozen: true
        };
        this.lastColumn = {
            componentType: "Label",
            Width: "100%",
            Label: '',
            Order: 10000
        };
    }
    /**
     * Resolves additional configurations or setup for the component.
     * @param {Component} com The component to configure.
     * @param {HTMLElement} [ele] Optional HTML element to use in the resolution.
     */
    Resolve(com, ele = null) {
        let txtArea = document.createElement('textarea');
        txtArea.innerHTML = ele.innerHTML;
        com.formatEntity = txtArea.value;
        ele.innerHTML = null;
    }

    /** @type {FeaturePolicy[]} */
    gridPolicies = [];
    /** @type {FeaturePolicy[]} */
    generalPolicies = [];
    /**
     * Renders the list view, setting up necessary configurations and data bindings.
     */
    Render() {
        if (this.editForm) {
            this.generalPolicies = this.editForm.Policies;
        }
        Html.take(this.parentElement).dataAttr('name', this.Name);
        this.addSections();
        this.setRowDataIfExists();
        if (this.meta.localRender) {
            this.localRender();
        }
        else {
            this.loadAllData();
        }
    }

    /**
     * Renders the list view either by re-rendering or using locally stored data based on the configuration.
     */
    localRender() {
        // Setting the header from the local metadata configuration
        this.Header = this.Header ?? this.meta.localHeader ?? this.meta.Columns;

        if (this.meta.localRender) {
            // If local rendering is enabled, re-render the view
            this.Rerender();
        } else {
            // If local rendering is not enabled, use the local data directly
            this.rowData.Data = this.meta.localData;
        }
    }

    Rerender() {
        this.mainSection.disposeChildren();
        Html.take(this.mainSection.element).clear();
        this.renderContent();
    }
    /**
     * Reloads data for the list view, potentially using cached headers and considering pagination settings.
     * @param {boolean} [cacheHeader=false] Specifies whether headers should be cached.
     * @param {number} [skip=null] Specifies the number of items to skip (for pagination).
     * @param {number} [pageSize=null] Specifies the size of the page to load.
     * @returns {Promise<any[]>} A promise that resolves to the list of reloaded data objects.
     */
    async reloadData(cacheHeader = false, skip = null, pageSize = null) {
        if (this.meta.componentType == "GridView" && !Utils.isNullOrWhiteSpace(this.meta.refName)) {
            Spinner.appendTo();
        }
        if (Utils.isNullOrWhiteSpace(this.meta.refName)) {
            const data = await new Promise((resolve) => {
                window.setTimeout(async () => {
                    var raw = Utils.isFunction(this.meta.Query, false, this);
                    await this.loadMasterData(raw);
                    this.setRowData(raw);
                    this.paginator.Show = false;
                    resolve(raw);
                }, 500);
            });

            return data;
        }
        if (this.meta.Editable && this.entity[this.meta.fieldName] && this.entity[this.meta.fieldName] instanceof Array && this.entity[this.meta.fieldName].length > 0) {
            var rows = this.entity[this.meta.fieldName];
            var rows = this.entity[this.meta.fieldName];
            if (!Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
                var rsObj = Utils.isFunction(this.meta.defaultVal, false, this);
                if (rsObj) {
                    rows.forEach(item => {
                        item["insertedBy"] = this.Token.userId;
                        Object.getOwnPropertyNames(rsObj).forEach(x => {
                            item[x] = rsObj[x];
                        });
                    })
                }
            }
            else {
                rows.forEach(item => {
                    item["insertedBy"] = this.Token.userId;
                })
            }
            await this.loadMasterData(rows);
            this.setRowData(rows);
            this.paginator.Show = false;
            Spinner.Hide();
            return rows;
        }
        if (this.paginator != null) {
            this.paginator.Options.pageSize = this.paginator.Options.pageSize === 0 ? (this.meta.row ?? 12) : this.paginator.Options.pageSize;
        }
        pageSize = (pageSize ?? this.paginator?.Options?.pageSize ?? this.meta.row) ?? 20;
        skip = !skip ? (this.paginator?.Options?.pageIndex * pageSize) : 0;
        let sql = this.getSql(skip, pageSize, cacheHeader);
        var rsLoad = await this.customQuery(sql);
        Spinner.Hide();
        return rsLoad;
    }

    async excelData() {
        let sql = this.getSql(0, 10000, false);
        sql.exportExcel = true;
        const data = await Client.instance.submitAsync({
            noQueue: true,
            Url: `/api/feature/com`,
            Method: "POST",
            jsonData: JSON.stringify(sql),
        });
        if (this.findClosest(x => x.isTabComponent)) {
            Client.download(data.Url, LangSelect.get(this.editForm.meta.Label, this.meta.Name) + "-" + LangSelect.get(this.Parent.meta.Label) + ".xlsx");

        }
        else {
            Client.download(data.Url, LangSelect.get(this.editForm.meta.Label, this.meta.Name) + ".xlsx");
        }
    }

    calcFilterQuery() {
        return this.listViewSearch.calcFilterQuery();
    }

    async exportExcel() {
        const htmlWithInline = await this.inlineAllStyles(this.element.outerHTML);
        const response = await Client.instance.submitAsync({
            noQueue: true,
            Url: `/api/htmlToExcel/export`,
            Method: "POST",
            jsonData: JSON.stringify({
                htmlTable: htmlWithInline,
                fileName: this.meta.plainText
            }),
        });

        let filename = this.meta.plainText || "Export.xlsx";
        const url = window.URL.createObjectURL(response);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
    }

    inlineAllStyles(html) {
        return new Promise((resolve) => {
            const iframe = document.createElement("iframe");
            iframe.style.display = "none";
            document.body.appendChild(iframe);

            const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;

            // Gán trực tiếp vào <html> thay vì dùng .write()
            iframe.onload = () => {
                iframeDoc.documentElement.innerHTML = html;

                const styles = [];

                for (const sheet of iframeDoc.styleSheets) {
                    try {
                        for (const rule of sheet.cssRules) {
                            styles.push(rule);
                        }
                    } catch (e) {
                        console.warn("Không thể truy cập stylesheet:", e);
                    }
                }

                styles.forEach(rule => {
                    if (!rule.selectorText || !rule.style) return;

                    const elements = iframeDoc.querySelectorAll(rule.selectorText);
                    elements.forEach(el => {
                        for (const prop of rule.style) {
                            const value = rule.style.getPropertyValue(prop);
                            const priority = rule.style.getPropertyPriority(prop);
                            el.style.setProperty(prop, value, priority);
                        }
                    });
                });

                const allElements = iframeDoc.querySelectorAll('*');
                allElements.forEach(el => el.removeAttribute('class'));

                const resultHtml = iframeDoc.documentElement.outerHTML;
                document.body.removeChild(iframe);
                resolve(resultHtml);
            };

            // Gán srcdoc để trigger iframe.onload
            iframe.srcdoc = html;
        });
    }

    /**
     * @param {Event} e
     */
    async exportExcelTemplate(e) {
        const wb = XLSX.utils.book_new();
        const ws_data = [];
        var headers = this.Header.filter(x => x.Label);
        ws_data.push(headers.map(x => x.Label));
        const ws = XLSX.utils.aoa_to_sheet(ws_data);
        XLSX.utils.book_append_sheet(wb, ws, 'templateImport');
        for (let index = 0; index < headers.length; index++) {
            if (headers[index].componentType == "Dropdown") {
                const ws_master_data = [];
                var headers_master = JSON.parse(headers[index].Template);
                ws_master_data.push(headers_master.map(x => x.Label));
                if (!headers[index].refName) {
                    const data = JSON.parse(headers[index].Query);
                    data.forEach(item => {
                        ws_master_data.push(headers_master.map(x => item[x.fieldName] || ""));
                    })
                    const ws_master = XLSX.utils.aoa_to_sheet(ws_master_data);
                    XLSX.utils.book_append_sheet(wb, ws_master, headers[index].Label);
                }
                else {
                    const data = await Client.instance.submitAsync({
                        noQueue: true,
                        Url: `/api/feature/com`,
                        Method: "POST",
                        jsonData: JSON.stringify({
                            comId: headers[index].Id,
                            Count: false,
                            Top: 100000,
                            Skip: 0,
                            orderBy: "ds.insertedDate asc",
                        }),
                    });
                    data.value.forEach(item => {
                        ws_master_data.push(headers_master.map(x => item[x.fieldName] || ""));
                    })
                    const ws_master = XLSX.utils.aoa_to_sheet(ws_master_data);
                    XLSX.utils.book_append_sheet(wb, ws_master, headers[index].Label);
                }

            }
        }
        XLSX.writeFile(wb, 'templateImport' + this.meta.Label + '.xlsx');
    }
    /**
     * @param {Event} e
     */
    async importExcelTemplate(e) {
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.xlsx, .xls';
        fileInput.addEventListener('change', (event) => {
            if (event.target.files.length > 0) {
                this.actImportExcelTemplate(event);
            }
        });
        fileInput.click();
    }
    /**
    * @param {Event} e
    */
    async actImportExcelTemplate(e) {
        try {
            const file = e.target.files[0];
            if (!file) {
                alert("No file selected.");
                return;
            }
            const reader = new fileReader();
            reader.onload = async (event) => {
                const data = new uint8Array(event.target.result);
                const workbook = XLSX.read(data, { type: "array" });
                const sheetName = workbook.sheetNames[0];
                const worksheet = workbook.Sheets[sheetName];
                const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 0 });
                if (jsonData.length === 0) {
                    alert("The selected file is empty or not in the expected format.");
                    return;
                }
                var masterName = [];
                var newObject = jsonData.map(rsObj => {
                    var item = {};
                    Object.getOwnPropertyNames(rsObj).forEach(x => {
                        var com = this.Header.find(y => y.Label == x);
                        if (com) {
                            item[com.fieldName] = rsObj[x];
                            if (!Utils.isNullOrWhiteSpace(com.refName)) {
                                masterName.push({
                                    tableName: com.refName,
                                    Format: com.formatData,
                                    Id: rsObj[x],
                                });
                            }
                        }
                    });
                    return item;
                });
                var groupedList = Object.values(masterName.reduce((acc, curr) => {
                    let existing = acc.find(item => item.tableName === curr.tableName);
                    if (!existing) {
                        existing = { tableName: curr.tableName, Format: curr.Format, Id: [] };
                        acc.push(existing);
                    }
                    if (!existing.Id.includes(curr.Id)) {
                        existing.Id.push(curr.Id);
                    }
                    return acc;
                }, []));
                let dataTasks = groupedList.map(x => ({
                    Header: x.tableName,
                    Data: Client.instance.getByNameAsync(x.tableName, x.Id, x.Format)
                }));
                let results = await Promise.all(dataTasks.map(x => x.Data));
                dataTasks.forEach((task, index) => {
                    task.Data = results[index];
                    if (!task.Data.data) {
                        return;
                    }
                    newObject.forEach(item => {
                        Object.getOwnPropertyNames(item).forEach(x => {
                            var com = this.Header.find(y => y.fieldName == x && y.refName == task.Header);
                            if (com) {
                                var format = com.formatData.replaceAll("{", "").replaceAll("}", "");
                                var mapItem = task.Data.data.find(x => x[format] == item[com.fieldName]);
                                if (mapItem) {
                                    item[com.fieldName] = mapItem.Id;
                                }
                                else {
                                    item[com.fieldName] = null;
                                }
                            }
                        });
                    });
                });
                for (let index = 0; index < newObject.length; index++) {
                    const element = newObject[index];
                    element["Id"] = Uuid7.newGuid();
                }
                this.editForm[this.meta.entityName || "Entity"][this.meta.fieldName] = newObject;
                await this.reloadData();
                this.Dirty = true;
                this.dispatchEvent(this.meta.Events, EventType.Change, this).then(() => {
                    resolve(false);
                });
            };
            reader.onerror = () => {
                alert("Error reading the file.");
            };
            reader.readAsArrayBuffer(file);
        } catch (error) {
            console.error("Import Error:", error.Message);
            alert("Failed to import file: " + error.Message);
        }
    }

    /** @type {Where[]} */
    Wheres = [];
    /**
     * Gets the SQL for data retrieval based on the current state of the list view.
     * @param {number} [skip=null] Number of records to skip for pagination.
     * @param {number} [pageSize=null] Page size for pagination.
     * @param {boolean} [cacheMeta=false] Whether to cache meta information.
     * @param {boolean} [count=true] Whether to include a count of total records.
     * @returns {SqlViewModel} The SQL view model with query details.
     */
    getSql(skip = null, pageSize = null, cacheMeta = false, count = true) {
        let submitEntity = Utils.isFunction(this.meta.preQuery, true, this);
        let basicCondition = this.calcFilterQuery();
        if (!submitEntity) {
            submitEntity = {};
        }
        var operatorsValue = basicCondition.filter(x => x.Value).map(item => {
            return {
                fieldName: item.fieldName,
                Value: item.Value,
            }
        }) || {};
        let finalCon = basicCondition
            .filter(x => x.Where)
            .map((x, index) => {
                return index === 0 ? x.Where : `${x.Operator} ${x.Where}`;
            })
            .join(" ");
        var orderby = this.searchSection.Children.filter(x => x.isOrderBy).map(x => "ds." + x.Meta.fieldName + " " + x.orderMethod).Combine(x => x, ", ");
        /** @type {SqlViewModel} */
        var res = {
            comId: this.meta.Id,
            Params: submitEntity ? JSON.stringify(submitEntity) : null,
            whereParams: JSON.stringify(operatorsValue),
            orderBy: !orderby ? (!this.meta.orderBy ? ((!this.meta.Editable || this.meta.componentType == "Dropdown") ? "ds.insertedDate desc" : "ds.insertedDate asc") : this.meta.orderBy) : orderby,
            Where: finalCon,
            Count: count,
            Skip: skip,
            Top: pageSize,
            skipXQuery: cacheMeta,
            metaConn: this.metaConn,
            dataConn: this.dataConn,
        };
        return res;
    }

    shouldSetEntity = true;
    /**
     * 
     * @param {any[]} listData 
     */
    setRowData(listData) {
        listData = listData ?? [];
        this.rowData._data = listData;
        this.renderContent();
    }

    /**
     * Executes a custom SQL query using the provided SQL view model.
     * @param {SqlViewModel} vm The view model containing SQL query details.
     * @returns {Promise<any[]>} A promise that resolves to the list of data objects retrieved.
     */


    async customQuery(vm) {
        const data = await Client.instance.submitAsync({
            noQueue: true,
            Url: `/api/feature/com`,
            Method: "POST",
            jsonData: JSON.stringify(vm),
        });
        if (!data.value || data.value.length === 0) {
            this.paginator.Show = false;
            this.clearRowData();
            this.setRowData([]);
            this.domLoaded();
            return [];
        }
        else {
            let total = data.count && data.count > 0 ? data.count : data.value.length;
            let rows = [...data.value];
            this.clearRowData();
            this.updatePagination(total, rows.length);
            await this.loadMasterData(rows);
            this.setRowData(rows);
            return rows;
        }
    }

    loadLocalData(rows) {
        var locals = this.Header.filter(x => ["Dropdown", "Select"].some(y => y == x.componentType) && Utils.isNullOrWhiteSpace(x.refName));
        for (const header of locals) {
            let containId = header.fieldName.substr(header.fieldName.length - 2) === this.idField;
            let objField = "";
            if (containId) {
                objField = header.fieldName.substr(0, header.fieldName.length - 2);
            }
            else {
                objField = header.fieldName + "masterData";
            }
            rows.forEach(row => {
                var data = Utils.isFunction(header.Query, false, this);
                let found = data.find(source => source[this.idField] === row[header.fieldName]);
                if (found) {
                    row[objField] = found;
                }
            });
        }
    }

    async loadMasterData(rows = null, spinner = true) {
        if (!Utils.isNullOrWhiteSpace(this.meta.groupBy)) {
            let keys = this.meta.groupBy.split(",");
            rows.forEach(item => {
                item[this._groupKey] = keys.map(key => item[key]).join(" ");
            });
            rows = rows.sort((a, b) => {
                if (a[this._groupKey] === b[this._groupKey]) {
                    return a.insertedDate - b.insertedDate;
                } else {
                    return a[this._groupKey] - b[this._groupKey];
                }
            });
        }
        var headers = this.Header.filter(x => !Utils.isNullOrWhiteSpace(x.refName));
        if (headers.length == 0) {
            this.loadLocalData(rows)
            return;
        }
        rows = rows || this.rowData.Data;
        let dataSource = headers.filter((obj, index, self) =>
            index === self.findIndex((t) => (
                t.refName === obj.refName
            ))
        ).map(x => this.formatDataSourceByEntity(x, headers, rows)).filter(x => x !== null);
        if (dataSource.length == 0) {
            this.loadLocalData(rows)
            return;
        }

        let dataTasks = dataSource.filter(x => x.dataSourceOptimized).map(x => ({
            tableName: x.refName,
            Ids: x.dataSourceOptimized,
            Header: x
        }));
        var results2 = await Client.instance.getByIdsAsync(dataTasks);
        results2.forEach((task, index) => {
            if (task && task.length == 0) {
                return;
            }
            this.setRemoteSource(task, dataTasks[index].Header.refName, dataTasks[index].Header);
        });
        this.syncMasterData(rows, headers);
    }

    setRemoteSource(remoteData, typeName, header) {
        let localSource = this.refData[typeName];
        if (!localSource) {
            this.refData[typeName] = remoteData;
        } else {
            remoteData.forEach(item => {
                if (!localSource.some(localItem => localItem[this.idField] === item[this.idField])) {
                    localSource.push(item);
                }
            });
        }
        var headers = this.Header.filter(x => x.refName == header.refName);
        headers.forEach(item => {
            item.localData = remoteData;
        })
    }

    syncMasterData(rows = null, headers = null) {
        rows = rows || this.rowData.Data;
        headers = headers || this.Header;

        headers.filter(x => x.refName).forEach(header => {
            if (!header.fieldName || header.fieldName.length <= 2) {
                return;
            }
            let containId = header.fieldName.substr(header.fieldName.length - 2) === this.idField;
            let objField = "";
            if (containId) {
                objField = header.fieldName.substr(0, header.fieldName.length - 2);
            }
            else {
                objField = header.fieldName + "masterData";
            }
            rows.forEach(row => {
                let propType = header.refName;
                if (!propType) {
                    return;
                }

                let propVal = row[objField];
                let found = this.refData[propType]?.find(source => source[this.idField] === row[header.fieldName]);
                if (found) {
                    row[objField] = found;
                } else if (propVal && !found) {
                    this.refData[propType] = this.refData[propType] || [];
                    this.refData[propType].push(propVal);
                }
            });
        });
        var locals = this.Header.filter(x => ["Dropdown", "Select"].some(y => y == x.componentType) && Utils.isNullOrWhiteSpace(x.refName));
        for (const header of locals) {
            let containId = header.fieldName.substr(header.fieldName.length - 2) === this.idField;
            let objField = "";
            if (containId) {
                objField = header.fieldName.substr(0, header.fieldName.length - 2);
            }
            else {
                objField = header.fieldName + "masterData";
            }
            rows.forEach(row => {
                var data = Utils.isFunction(header.Query, false, this);
                if (data) {
                    let found = data.find(source => source[this.idField] === row[header.fieldName]);
                    if (found) {
                        row[objField] = found;
                    }
                }
            });
        }
    }

    formatDataSourceByEntity(currentHeader, allHeaders, entities) {
        let entityIds = allHeaders
            .filter(x => x.refName === currentHeader.refName)
            .flatMap(x => this.getEntityIds(x, entities))
            .filter((v, i, a) => a.indexOf(v) === i);

        if (entityIds.length === 0) {
            return null;
        }

        currentHeader.dataSourceOptimized = entityIds.sort();
        return currentHeader;
    }

    getEntityIds(header, entities) {
        if (!entities || entities.length === 0) {
            return [];
        }

        let ids = [];
        entities.forEach(x => {
            let id = !x[header.fieldName] ? null : x[header.fieldName].toString();
            if (!id) {
                return;
            } else if (id.includes(',')) {
                ids.push(...id.split(',').map(y => y));
            } else {
                ids.push(id);
            }
        });
        return ids;
    }

    /**
     * Updates pagination details based on total data and current page count.
     * @param {number} total The total number of records.
     * @param {number} currentPageCount The number of records in the current page.
     */
    updatePagination(total, currentPageCount) {
        if (!this.paginator) {
            return;
        }
        let options = this.paginator.Options;
        options.Total = total;
        options.currentPageCount = currentPageCount;
        options.pageNumber = options.pageIndex + 1;
        options.startIndex = options.pageIndex * options.pageSize + 1;
        options.endIndex = options.startIndex + options.currentPageCount - 1;
        this.paginator.updateView();
    }

    /**
     * Adds sections to the ListView based on the component configurations.
     */
    addSections() {
        if (this.meta.liteGrid) {
            this.element = this.parentElement;
            this.element.innerHTML = null;
            this.mainSection = new ListViewSection(null, this.parentElement);
            this.addChild(this.mainSection);
            return;
        }
        Html.take(this.parentElement).div.className("grid-wrapper");
        this.element = Html.context;
        if (this.meta.canSearch) {
            Html.instance.div.div.className("grid-toolbar search").end.render();
            Html.instance.div.className("button-toolbar").end.end.render();
        }
        this.listViewSearch = new ListViewSearch(this.meta);
        this.addChild(this.listViewSearch);
        Html.take(this.element).div.className("list-content").end.div.className("empty");
        this.emptySection = new ListViewSection(null, Html.context);
        this.emptySection.parentElement = this.element;
        this.addChild(this.emptySection);

        // @ts-ignore
        this.mainSection = new ListViewSection(null, this.emptySection.element.previousElementSibling);
        this.addChild(this.mainSection);

        Html.instance.endOf(".list-content");
        this.renderPaginator();
    }

    /** @type {any[]} */
    formattedRowData = [];
    /**
     * Renders the content within the main section of the ListView.
     */
    renderContent() {
        this.mainSection.disposeChildren();
        this.emptySection?.disposeChildren();
        this.formattedRowData = this.formattedRowData.length == 0 ? this.rowData.Data : this.formattedRowData;
        if (this.formattedRowData.length == 0) {
            return;
        }
        this.formattedRowData.forEach((rowData, index) => {
            this.renderRowData(this.Header, rowData, this.mainSection);
        });
        this.contentRendered();
    }

    /**
     * Renders the data for each row within the list view.
     * @param {Component[]} headers The headers to use in the row.
     * @param {object} row The data object for the row.
     * @param {ListViewSection} section The section where the row is to be added.
     * @param {number} [index=null] Optional index for the row.
     * @param {boolean} [emptyRow=false] Indicates if the row is empty.
     * @returns {ListViewItem} The ListViewItem created for the row.
     */
    renderRowData(headers, row, section, index = null, emptyRow = false) {
        let rowSection = this.meta.liteGrid ? new ListViewItem() : new ListViewItem('div');
        rowSection.emptyRow = emptyRow;
        rowSection.entity = row;
        rowSection.parentElement = section.element;
        rowSection.listView = this;
        rowSection.listViewSection = section instanceof ListViewSection ? section : null;
        rowSection.meta = this.meta;
        rowSection.editForm = this.editForm;
        section.addChild(rowSection, index);
        rowSection.renderRowData(headers, row, index, emptyRow);
        return rowSection;
    }

    /**
     * Clears all row data from the ListView.
     */
    clearRowData() {
        this.rowData.clear();
        this.mainSection.Children.forEach(x => x.Dispose());
        this.mainSection.Children = [];
        this.mainSection.element.innerHTML = null;
        this.formattedRowData = [];
        if (this.entity == null || this.Parent.isSearchEntry) {
            return;
        }
        if (this.shouldSetEntity) {
            this.entity[this.Name] = this.rowData.Data;
        }
    }

    /** @type {ListViewItem[]} */
    // @ts-ignore
    get allListViewItem() { return this.mainSection.Children; }
    /** @return {ListViewItem[]} */
    get Item() { return this.mainSection.Children.filter(x => !x.groupRow); }
    /**
     * Performs an action on all items that meet the condition specified by predicate.
     * @param {(item: EditableComponent) => void} action - The action to perform on each ListViewItem that meets the condition.
     * @param {(item: EditableComponent) => boolean} predicate - The condition to check each ListViewItem.
     */
    rowAction(action, predicate = null) {
        this.allListViewItem.filter(x => !predicate || predicate(x)).forEach(action);
    }

    /**
     * Sets row data if the entity exists and it is not an empty string.
     */
    setRowDataIfExists() {
        const value = Utils.getPropValue(this.entity, this.Name);
        if (this.entity != null && Array.isArray(value)) {
            this.rowData._data = value;
        }
    }

    /**
     * Method to update the view of the ListView, possibly forcing the update and setting the dirty flag.
     * @param {boolean} [force=false] Whether to force the update.
     * @param {boolean|null} [dirty=null] Optional dirty flag to set.
     * @param {string[]} componentNames Component names to specifically update.
     */
    updateView(force = false, dirty = null, componentNames = []) {
        if (!this.Editable) {
            if (force) {
                this.listViewSearch.refreshListView();
            }
        } else {
            this.rowAction(row => row.updateView(force, dirty, componentNames), row => !row.emptyRow);
        }
    }

    /**
     * Adds a new empty row to the ListView.
     */
    addNewEmptyRow() {
        if (this.disabled || !this.meta.canAdd) {
            return;
        }
        let emptyRowData = {};
        let dfObj = Utils.isFunction(this.meta.defaultVal, false, this);
        if (dfObj) {
            Object.keys(dfObj).forEach(key => {
                emptyRowData[key] = dfObj[key];
            });
        }
        emptyRowData[this.idField] = null;
        this.renderRowData(this.Header, emptyRowData, this.emptySection, null, true);
        if (!this.meta.topEmpty) {
            this.mainSection.element.insertBefore(this.mainSection.element, this.emptySection.element);
        } else {
            this.mainSection.element.appendChild(this.emptySection.element.firstElementChild);
        }
        this.dispatchCustomEvent(this.meta.Events, customEventType.afterEmptyRowCreated, emptyRowData).Done();
    }

    /**
     * Renders the paginator component if necessary based on the configuration and data.
     */
    renderPaginator() {
        if (this.meta.localRender || this.meta.liteGrid) {
            if (this.paginator) {
                this.paginator.Show = false;
            }
            return;
        }
        if (!this.meta.row || this.meta.row === 0) {
            this.meta.row = 20;
        }

        if (!this.paginator) {
            // @ts-ignore
            this.paginator = new Paginator({
                total: 0,
                pageSize: this.meta.row ?? 50,
                currentPageCount: this.rowData.data.length,
                pageIndex: 0,
                pageIndex: 0,
            });
            this.addChild(this.paginator);
        }
    }

    get updatedRows() {
        return this.allListViewItem.orderBy(x => x.rowNo).where(x => x.Dirty).select(x => x.Entity).distinct();
    };

    get updatedListItems() {
        return this.allListViewItem.orderBy(x => x.rowNo).where(x => x.Dirty);
    };

    /**
     * Retrieves a list of patches if there are updates, optionally updating the view.
     * @param {boolean} [updateView=false] - Indicates whether the view should be updated.
     * @returns {PatchVM[] | null} An array of PatchVM instances or null if no updates are dirty.
     */
    getPatches(updateView = false) {
        if (!this.Dirty) {
            return null;
        }

        if (this.meta.idField !== null && this.meta.idField !== this.idField) {
            this.updatedRows.forEach(row => {
                row[this.meta.idField] = this.entityId;
            });
        }

        const res = [];
        this.updatedListItems.forEach(item => {
            res.push(item.getPatchEntity());
        });

        if (updateView) {
            this.updateView();
        }

        return res;
    }

    /**
     * Filters and sorts the header components based on their properties.
     * @param {Component[]} components The list of components to filter.
     * @returns {Component[]} The filtered and sorted list of header components.
     */
    filterColumns(components) {
        if (!components || components.length === 0) return components;
        const headers = this.editForm.getComPolicies(components).map(x => {
            x.entityName = this.meta.entityName;
            return this.calcTextAlign(x);
        }).sort((a, b) => (b.componentType === "Button" ? 1 : 0) - (a.componentType === "Button" ? 1 : 0) || a.Order - b.Order)
        this.orderHeaderGroup(headers);
        this.Header = [];
        if (!["Dropdown", "Select"].some(x => x === this.meta.componentType)) {
            this.Header.push(this.toolbarColumn);
        }
        this.Header.push(...headers);
        if (!["Dropdown", "Select"].some(x => x === this.meta.componentType)) {
            this.Header.push(this.lastColumn);
        }
        this.Header = this.Header.filter(x => x !== null && !x.topEmpty);
        return this.Header;
    }

    /**
     * Applies a filter to the ListView, reloading data based on the current filter settings.
     * @returns {Promise} A promise that resolves once the data has been reloaded with the applied filter.
     */
    applyFilter() {
        this.clearRowData();
        return this.reloadData(true, 0);
    }

    getSelectedRows() {
        return this.allListViewItem.filter(x => !x.groupRow && x.Selected).map(x => x.entity);
    }

    itemSelected() {
        return this.allListViewItem.filter(x => !x.groupRow && x.Selected).map(x => x.entity);
    }

    getRowFocus() {
        return this.allListViewItem.filter(x => !x.groupRow && x.Focused);
    }

    getRowSelected() {
        return this.allListViewItem.find(x => !x.groupRow && x.Selected);
    }

    bodyContextMenuShow = new Action();
    /**
     * Handles the context menu for the body of the list view, showing additional options.
     * @param {Event} e The event object associated with the context menu action.
     */
    bodyContextMenuHandler(e) {
        e.preventDefault();
        e.stopPropagation();
        this.setSelected(e);
        ContextMenu.Instance.menuItems.clear();
        let ctxMenu = ContextMenu.Instance;
        var addFn = Utils.isFunction(this.meta.addRowExp, false, this);
        var some = this.allListViewItem.some(x => x.Selected && (x.entity["assignId"] == this.Token.userId || x.entity["insertedBy"] == this.Token.userId)) || (this.meta.Editable && this.editForm.entityId && this.editForm.entityId.startsWith("-")) || e.target.closest('.tb-empty') != null || addFn;
        if ((this.disabled || (!this.meta.canWrite && !some)) || (this.meta.canWrite && !this.meta.canWriteAll && !some) && this.editForm.entityId) {
            ContextMenu.Instance.menuItems.push({
                Icon: "fal fa-undo",
                Text: "Reload",
                Shortcut: "Ctrl+R",
                Line: true,
                Click: () => this.actionFilter()
            });
            ctxMenu.Top = e.Top();
            ctxMenu.Left = e.Left();
            ctxMenu.editForm = this.editForm;
            ctxMenu.render();
            document.body.appendChild(ctxMenu.element);
            ctxMenu.element.style.position = "absolute";
            return;
        }
        this.bodyContextMenuShow?.invoke();
        this.dispatchEvent(this.meta.Events, EventType.contextMenu, this, ctxMenu).then(() => {
            this.renderCopyPasteMenu(this.Editable);
            this.renderEditMenu();
            ctxMenu.Top = e.Top();
            ctxMenu.Left = e.Left();
            ctxMenu.editForm = this.editForm;
            ctxMenu.render();
            document.body.appendChild(ctxMenu.element);
            ctxMenu.element.style.position = "absolute";
        });
    }

    async renderRelatedDataMenu() {
        const targetRef = await Client.instance.getByIdAsync('EntityRef', this.dataConn, [this.meta.Id]);
        if (targetRef.nothing()) {
            return;
        }
        const menuItems = targetRef.select(x => ({
            Text: x.menuText,
            Click: (arg) => this.openFeature(x),
        })).toList();
        // @ts-ignore
        ContextMenu.Instance.menuItems.push({
            Icon: "fal fal fa-ellipsis-h",
            Text: "Dữ liệu liên quan",
            menuItems: menuItems
        });
    }

    // /** @type {EditableComponent[]} */
    /** @type {CellSelected[]} */
    CellSelected = [];
    /**
     * Applies filtering logic to the ListView based on the EntityRef.
     * It finds a specific GridView based on EntityRef, clears its conditions and dates,
     * and then updates it with new selected conditions.
     *
     * @param {TabEditor} tab - The TabEditor instance.
     * @param {EntityRef} entityRef - The EntityRef containing filtering criteria.
     */
    Filter(tab, entityRef) {
        /** @type {GridView} */
        // @ts-ignore
        let gridView1 = tab.filterChildren(x => x instanceof EditableComponent.gridViewMd.gridView).find(X => X.meta.Id === entityRef.targetComId);
        if (!gridView1) {
            return;
        }

        gridView1.cellSelected = [];
        gridView1.advSearchVM.Conditions = [];
        gridView1.listViewSearch.entityVM.startDate = null;
        gridView1.listViewSearch.entityVM.endDate = null;

        this.getRealTimeSelectedRows().then(Selecteds => {
            let Com = gridView1.Header.find(X => X.fieldName === entityRef.targetFieldName);
            if (!Com) return;

            let cellSelecteds = Selecteds.map(Selected => ({
                fieldName: entityRef.targetFieldName,
                fieldText: Com.Label,
                componentType: Com.componentType,
                Value: Selected[entityRef.fieldName].toString(),
                valueText: Selected[entityRef.fieldName].toString(),
                Operator: operatorEnum.In,  // Assuming operatorEnum is predefined
                operatorText: "Contains",
                Logic: 'Or',
                isSearch: true,
                Group: true
            }));

            gridView1.cellSelected.push(...cellSelecteds);
            gridView1.actionFilter();
        });
    }

    /**
     * Sets the row as selected based on the event target.
     * @param {Event} e The event object.
     */
    setSelected(e) {
        // @ts-ignore
        let target = e.target.closest('tr');
        /** @type {ListViewItem} */
        // @ts-ignore
        let currentRow = this.mainSection.Children.find(x => x.element === target);
        if (currentRow) {
            if (!currentRow.groupRow || this.meta.groupReferenceId) {
                if (this.selectedIds.length === 1) {
                    this.clearSelected();
                }
                currentRow.Selected = true;
                this.lastListViewItem = currentRow;
                this.selectedIndex = currentRow.rowNo;
            }
        }
    }

    /**
     * Renders the pagination details and handles the data loading process.
     */
    loadAllData() {
        this.loadHeader().then(() => {
            this.reloadData(true).then();
        });
    }

    Dropdown = "Dropdown";

    async loadHeader() {
        var columns = this.loadGridPolicy().length > 0 ? this.loadGridPolicy() : (this.meta.Columns ?? []);
        var pivotRow = columns.find(x => x.componentType == "Number" && x.isMultiple && x.groupFormat);
        if (pivotRow) {
            var index = columns.indexOf(pivotRow);
            const submitEntity = Utils.isFunction(pivotRow.preQuery, false, this);
            const entity = {
                Params: submitEntity ? JSON.stringify(submitEntity) : null,
                comId: pivotRow.Id,
            };
            var data = await Client.instance.submitAsync({
                Url: "/api/feature/sql",
                isRawString: true,
                jsonData: JSON.stringify(entity),
                Method: "POST"
            });
            var pivotData = data[0];
            var headers = pivotData.map(x => ({
                fieldName: x.Id,
                Label: Utils.formatEntity(pivotRow.Label, x),
                componentType: "Number",
                isChild: true,
                Order: pivotRow.Order + 1,
                Width: pivotRow.Width,
                minWidth: pivotRow.minWidth,
                maxWidth: pivotRow.maxWidth,
                groupName: x.codeMn,
                Summary: pivotRow.Summary,
                summaryColSpan: pivotRow.summaryColSpan,
                groupFormat: `const vndAmount = this.childrenItems
                    .map(x => x.Entity).reduce((a, b) => a.plus(b["${x.Id}"] || 0), new this.Decimal(0))
                        .toFixed(0)
                        .replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                return vndAmount == '0' ? '' : vndAmount;`
            }));
            if (pivotRow.isSumary) {
                headers.push({
                    fieldName: "Total",
                    isTotal: true,
                    Label: pivotRow.groupName,
                    componentType: "Number",
                    Order: pivotRow.Order + 2,
                    Width: pivotRow.Width,
                    childHeader: headers,
                    minWidth: pivotRow.minWidth,
                    maxWidth: pivotRow.maxWidth,
                    formatData: pivotRow.formatData,
                    groupFormat: pivotRow.groupFormat,
                    Summary: pivotRow.Summary,
                    summaryColSpan: pivotRow.summaryColSpan,
                });
            }
            columns.splice(index, 1, ...headers);
        }
        this.dispatchCustomEvent(this.meta.Events, customEventType.updateHeader, columns);
        if (!this.meta.Columns) {
            columns = this.filterColumns(columns);
        }
        this.Header = columns;
    }

    isMouseDown = false;
    /**
     * @type {HTMLElement}
     */
    startCell = null;
    startCellElement = null;
    Matrix = [];
    loadGridPolicy() {
        var sysSetting = [];
        if (this.meta.Columns?.length > 0) {
            sysSetting = this.meta.Columns;
        }
        else {
            if (!Utils.isNullOrWhiteSpace(this.meta.Template) && ["Dropdown", "Select"].some(x => x == this.meta.componentType)) {
                sysSetting = JSON.parse(this.meta.Template, null, 2);
                sysSetting.forEach((x, index) => {
                    x.Active = true;
                    x.virtualScroll = true;
                    x.Order = index + 1;
                });
            }
            else {
                sysSetting = this.editForm.meta.gridPolicies.filter(x => x.entityId == this.meta.fieldName);
            }
        }
        if (this.editForm.meta.userSettings) {
            var userSetting = this.editForm.meta.userSettings.find(x => x.componentId == this.meta.Id);
            if (userSetting) {
                var policys = JSON.parse(userSetting.Value);
                sysSetting.forEach(item => {
                    var map = policys.find(x => x.fieldName == item.fieldName);
                    if (map) {
                        item.Width = map.Width;
                        item.minWidth = map.Width;
                        item.maxWidth = map.Width;
                        item.Order = map.Order || item.Order;
                    }
                });
            }
        }
        return sysSetting;
    }
    notCellText = ["Button", "Image", "imageUploader"]
    /**
     * Filters the columns based on the header configuration and applies sort order.
     */
    orderHeaderGroup(headers) {
        for (let i = 0; i < headers.length; i++) {
            for (let j = i + 1; j < headers.length; j++) {
                if (headers[i].groupName && headers[i].groupName === headers[j].groupName && headers[i + 1].groupName !== headers[j].groupName) {
                    let temp = headers[i + 1];
                    headers[i + 1] = headers[j];
                    headers[j] = temp;
                }
            }
        }
    }

    /** @type {any[]} */
    _copiedRows;

    /**
    * Copies the selected rows.
    * @param {object} ev The event object.
    */
    copySelected(ev) {
        var selected = this.getSelectedRows();
        var dataCopy = {
            tableName: this.meta.refName,
            rowData: this.copyRowWithoutId(selected)
        }
        this.copyData = dataCopy;
        this.dispatchCustomEvent(this.meta.Events, customEventType.afterCopied, selected, this._copiedRows);
    }

    set copyData(data) {
        return window["copyData"] = data;
    }

    get copyData() {
        var dataCop = window["copyData"];
        if (!dataCop) {
            return null;
        }
        return dataCop;
    }

    deepCopy(obj, path = null) {
        return JSON.parse(JSON.stringify(obj)); // Simple deep copy implementation
    }

    setPropValue(obj, propName, value) {
        obj[propName] = value;
    }

    getPropValue(obj, propName) {
        return obj[propName];
    }

    processObjectRecursive(obj, callback) {
        for (let key in obj) {
            if (obj.hasOwnProperty(key)) {
                callback(obj);
                if (typeof obj[key] === 'object' && obj[key] !== null) {
                    this.processObjectRecursive(obj[key], callback);
                }
            }
        }
    }

    copyRowWithoutId(selectedRows, path = null) {
        return selectedRows.map(row => {
            let res = this.deepCopy(row, path);
            this.setPropValue(res, this.idField, Uuid7.newGuid());
            this.setPropValue(res, this.statusIdField, 1);
            if (res["noSubmit"] != undefined || res["noSubmit"] != null) {
                this.setPropValue(res, "parentId", null);
            }
            this.resetObject(res);
            this.processObjectRecursive(res, obj => {
                let id = this.getPropValue(obj, this.idField);
                if (id && id > 0) {
                    this.setPropValue(obj, this.idField, 0);
                }

                let status = this.getPropValue(obj, this.statusIdField);
                if (status !== undefined) {
                    this.setPropValue(obj, this.statusIdField, 1);
                }
            });
            return res;
        });
    }

    /**
    * Pastes the copied rows.
    * @param {object} ev The event object.
    */
    async pasteSelected(ev) {
        var dataCopy = this.copyData;
        if (!dataCopy || dataCopy.tableName != this.meta.refName) {
            return;
        }
        var copyRows = dataCopy.rowData;
        if (copyRows.length == 0) {
            return;
        }
        copyRows.forEach(cell => {
            cell["disableRow"] = false;
            if (!Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
                var rsObj = Utils.isFunction(this.meta.defaultVal, false, this);
                if (rsObj) {
                    Object.getOwnPropertyNames(rsObj).forEach(x => {
                        cell[x] = rsObj[x];
                    });
                }
            }
        });
        Toast.Success("Copying...");
        this.dispatchCustomEvent(this.meta.Events, customEventType.beforePasted, copyRows).then(() => {
            var index = this.allListViewItem.reduceRight((acc, x2, index) => {
                if (acc === -1 && x2.Selected) {
                    return index;
                }
                return acc;
            }, -1);
            this.addRowsNo(copyRows, index).then(list => {
                if (this.meta.isRealtime) {
                    Promise.all(list.select(x => x.patchUpdateOrCreate())).then(() => {
                        this.copyData = [];
                        Toast.Success("Data pasted successfully !");
                        super.Dirty = false;
                        this.clearSelected();
                    });
                }
                else {
                    this.copyData = [];
                    Toast.Success("Data pasted successfully !");
                }
                this.dispatchCustomEvent(this.meta.Events, customEventType.afterPasted, copyRows).then();
            });
        });
    }

    /**
     * Renders menus related to the data linked with the selected rows, such as copy, paste, and editing options.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    renderCopyPasteMenu(canWrite) {
        ContextMenu.Instance.menuItems.push({
            Icon: "fal fa-undo",
            Text: "Reload",
            Shortcut: "Ctrl+R",
            Line: true,
            Click: () => this.actionFilter()
        });
        if (this.meta.canAdd) {
            ContextMenu.Instance.menuItems.push({
                Icon: "fal fa-copy",
                Text: "Copy",
                Click: () => this.copySelected()
            });
            ContextMenu.Instance.menuItems.push({
                Icon: "fal fa-clone",
                Text: "Copy & Paste",
                Shortcut: "Ctrl+U",
                Click: () => this.duplicateSelected(null, false)
            });
        }
        var dataCopy = this.copyData;
        if (dataCopy && dataCopy.tableName == this.meta.refName) {
            if (canWrite && dataCopy.rowData.length > 0) {
                ContextMenu.Instance.menuItems.push({
                    Icon: "fal fa-paste",
                    Text: "Paste",
                    Click: () => this.pasteSelected()
                });
            }
        }
    }

    /**
     * Renders edit menu options based on user permissions.
     * @param {boolean} canWrite Indicates whether the user has write permissions.
     */
    renderEditMenu() {
        if (this.meta.canDeactivate) {
            ContextMenu.Instance.menuItems.push({
                Icon: "fal fa-unlink",
                Text: "Deactivate",
                Click: () => this.deactivateSelected()
            });
        }
        var selected = this.getSelectedRows();
        var check = selected.some(x => (x["statusId"] && !x["progressId"] && [2, 3].includes(x["statusId"]) && !x["noApproved"] && !x["isUse"]) || (x["progressId"] && [2, 3].includes(x["progressId"])) || x["noSubmit"] || x["isLock"] || x["isPayment"] || x["isInvoice"] || x["isPaymentAcc"] || x["isDebtAcc"]);
        if (this.meta.canDelete && !check) {
            if (this.meta.canDeleteAll) {
                ContextMenu.Instance.menuItems.push({
                    Icon: "fal fa-trash",
                    Text: "Delete Data",
                    Shortcut: "F8",
                    Line: true,
                    Click: () => this.hardDeleteSelected()
                });
            }
            else {
                check = selected.some(x => x["insertedBy"] && x["insertedBy"] == this.Token.userId || x["assignId"] == this.Token.userId);
                if (check) {
                    ContextMenu.Instance.menuItems.push({
                        Icon: "fal fa-trash",
                        Text: "Delete Data",
                        Line: true,
                        Shortcut: "F8",
                        Click: () => this.hardDeleteSelected()
                    });
                }
            }
        }
        if (this.meta.canRead) {
            ContextMenu.Instance.menuItems.push({
                Icon: "fal fa-history",
                Text: "View History",
                Click: async () => await this.viewHistory()
            });
        }
    }
    canDelete;
    disposeViewHistory() {
        this._history.innerHTML = null;
    }

    /**
     * Renders the view history popup for the selected row.
     * @param {object} currentItem The currently selected row item.
     */
    async viewHistory(currentItem) {
        const selectedRows = this.getSelectedRows();
        currentItem = selectedRows[0];
        if (!currentItem) {
            return;
        }
        Html.take(this.tabEditor.element).div.className("backdrop").tabIndex(-1).trigger(EventType.Focus)
            .style("align-items: baseline;");
        this._history = Html.context;
        Html.instance.div.escape((e) => this.disposeViewHistory.bind(this)).className("popup-content confirm-dialog history-view")
            .div.className("popup-title").innerHTML("View history change")
            .div.className("icon-box").span.className("fal fa-times")
            .event(EventType.Click, () => this._history.remove())
            .endOf(".popup-title")
            .div.className("card-body panel group");
        const body = Html.context;
        var coms = await Client.instance.getService("History Change");
        var com = coms[0][0];
        com.Row = 50;
        var params = {
            recordId: currentItem.Id,
            tableName: this.meta.refName,
        }
        com.Columns = [
            {
                statusBar: true,
                Order: 0,
                Label: '',
                Frozen: true
            },
            {
                fieldName: "textContent",
                Order: 1,
                componentType: "Input",
                Label: "History",
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                Width: "80%",
                minWidth: "80%",
                maxWidth: "80%",
            },
            {
                fieldName: "insertedBy",
                componentType: "Dropdown",
                refName: "User",
                Order: 2,
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                formatData: `<div class="user-avatar">
                    <img src="{Avatar}" alt="{fullName}" class="avatar">
                    <a class="full-name">{fullName}</a>
                </div>`,
                Label: "Inserted By",
                Width: "10%",
                minWidth: "10%",
                maxWidth: "10%",
            },
            {
                fieldName: "insertedDate",
                Order: 3,
                canRead: true,
                canWrite: true,
                canReadAll: true,
                canWriteAll: true,
                componentType: "Datepicker",
                formatData: "DD/MM/YYYY HH:mm",
                Label: "Inserted Date",
                Width: "10%",
                minWidth: "10%",
                maxWidth: "10%",
            }
        ]
        com.preQuery = JSON.stringify(params);
        com.canSearch = false;
        const md = await import('./gridView.js');
        const _filterGrid = new md.gridView(com);
        _filterGrid.canDelete = false;
        _filterGrid.parentElement = body;
        this.tabEditor.addChild(_filterGrid);
        _filterGrid.element.style.width = "100%";
        _filterGrid.element.style.height = "calc(100vh - 22rem)";
    }

    /** @type {FeaturePolicy[]} */
    recordPolicy = [];
    static isOwner = '__IsOwner';
    /**
     * Handles the event for selected row deactivation.
     */
    async deactivateSelected() {
        const confirmDialog = new ConfirmDialog();
        confirmDialog.Content = "Are you sure you want to deactivate?"
        confirmDialog.editForm = this;
        confirmDialog.render();
        confirmDialog.yesConfirmed += async () => {
            confirmDialog.Dispose();
            const deactivatedIds = await this.Deactivate();
            this.dispatchCustomEvent(this.meta.Events, customEventType.deactivated, this.entity);
        };
    }
    /**
     * Deactivates selected rows by their iDs.
     * @returns {Promise<Array<string>>} A promise that resolves to an array of deactivated iDs.
     */
    async Deactivate() {
        const ids = this.getSelectedRows().map(x => x[this.idField].toString());
        const deactivatedIds = await Client.instance.deactivateAsync(ids, this.meta.refName, this.dataConn);
        if (deactivatedIds.length > 0) {
            Toast.Success("Data deactivated successfully");
        } else {
            Toast.Warning("An error occurred during deactivation");
        }
        return deactivatedIds;
    }
    /**
     * Handles deleting selected rows after confirming the action.
     */
    async hardDeleteSelected() {
        var deletedItems = [];
        deletedItems = this.getSelectedRows();
        var check = deletedItems.some(x => (x["statusId"] && [2, 3].includes(x["statusId"]) && !x["noApproved"] && !x["isUse"]) || x["noSubmit"] || x["isLock"] || x["isPayment"] || x["isInvoice"] || x["isPaymentAcc"] || x["isDebtAcc"]);
        if (deletedItems.length == 0 || check || this.meta.isMultiple || !this.meta.canDelete) {
            return;
        }
        const confirmDialog = new ConfirmDialog();
        confirmDialog.Title = "Are you sure you want to delete the selected rows?";
        confirmDialog.pElement = this.editForm.element;
        confirmDialog.editForm = this.editForm;
        confirmDialog.render();
        confirmDialog.yesConfirmed.add(() => {
            const cleaned = this.meta.Query.replace(/[\u0000-\u001F]+/g, '')
            var jsonQuery = JSON.parse(cleaned);
            if (jsonQuery && jsonQuery.delete) {
                const ids = deletedItems.map(x => x[this.idField]).filter(x => !x.startsWith('-'));
                if (ids && ids.length > 0) {
                    let submitEntity = Utils.isFunction(this.meta.preQuery, true, this);
                    Client.instance.postAsync({
                        entityIds: ids,
                        Params: submitEntity ? JSON.stringify(submitEntity) : null,
                        comId: this.meta.Id
                    }, "/api/checkDelete").then((rs) => {
                        if (rs.status == 200) {
                            this.hardDeleteConfirmed(deletedItems).then(async (rs) => {
                                if (rs) {
                                    await this.dispatchCustomEvent(this.meta.Events, customEventType.afterDeleted, this, deletedItems);
                                    await this.dispatchCustomEvent(this.meta.Events, EventType.Change, this);
                                }
                            });
                        }
                        else {
                            if (rs.Message) {
                                this.editForm.openConfig(rs.Message, () => {
                                }, () => { }, false, [], true)
                            }
                            else {
                                var searchEntry = JSON.parse(JSON.stringify(this.meta));
                                searchEntry.componentType = "Dropdown";
                                searchEntry.Events = null;
                                searchEntry.Style = null;
                                searchEntry.childStyle = null;
                                searchEntry.canSearch = false;
                                searchEntry.fieldName = "newEntityId";
                                this.editForm.openConfig("Please select a replacement data.", () => {
                                    this.hardDeleteConfirmed(deletedItems, this.editForm.entity.newEntityId).then(async rs => {
                                        if (rs) {
                                            this.editForm.entity.newEntityId = null;
                                            await this.dispatchCustomEvent(this.meta.Events, customEventType.afterDeleted, this, deletedItems);
                                            await this.dispatchCustomEvent(this.meta.Events, EventType.Change, this);
                                            this.actionFilter();
                                        }
                                    });
                                }, () => { }, true, [searchEntry])
                            }
                        }
                    });
                }
                else {
                    this.hardDeleteConfirmed(deletedItems).then(async rs => {
                        if (rs) {
                            await this.dispatchCustomEvent(this.meta.Events, customEventType.afterDeleted, this, deletedItems);
                            await this.dispatchCustomEvent(this.meta.Events, EventType.Change, this);
                        }
                    });
                }
            }
            else {
                this.hardDeleteConfirmed(deletedItems).then(async rs => {
                    if (rs) {
                        await this.dispatchCustomEvent(this.meta.Events, customEventType.afterDeleted, this, deletedItems);
                        await this.dispatchCustomEvent(this.meta.Events, EventType.Change, this);
                    }
                });
            }
        });
    }

    /**
     * Confirms the deletion of selected rows and performs the deletion.
     * @param {Array<object>} deletedItems Items to be deleted.
     * @returns {Promise<Array<object>>} A promise that resolves to the array of deleted items.
     */
    async hardDeleteConfirmed(deletedItems, newId) {
        const ids = deletedItems.map(x => x[this.idField]).filter(x => !x.startsWith('-'));
        if (this.meta.Editable) {
            ids.forEach(x => {
                this.deleteTempIds.push(x);
            });
            this.allListViewItem.filter(x => x.Selected).forEach(x => {
                x.Dispose();
                if (x.groupSection && x.groupSection.childrenItems.length > 0) {
                    const index = x.groupSection.childrenItems.indexOf(x);
                    if (index > -1) {
                        x.groupSection.childrenItems.splice(index, 1);
                    }
                    if (x.groupSection.childrenItems.length == 0) {
                        x.groupSection.Dispose();
                    }
                }
            });
            this.clearSelected();
            this.Dirty = true;
            Toast.Success("Deleted successfully");
            return true;
        }
        else {
            const result = await Client.instance.hardDeleteAsync(ids, this.meta.refName, newId, this.meta.Id);
            if (result) {
                this.allListViewItem.filter(x => x.Selected).forEach(x => x.Dispose());
                this.clearSelected();
                if (this.meta.isRealtime) {
                    this.Dirty = false;
                }
                Toast.Success("Deleted successfully");
                return true;
            } else {
                this.editForm.openConfig("The selected data cannot be deleted. Please check the data.", () => {
                }, () => { }, false, [], true);
                return false;
            }
        }
    }

    /**
     * Duplicates the selected rows and optionally adds a new row based on the duplicate.
     * @param {Event} ev The event object (not used in this method).
     * @param {boolean} addRow Whether to add a new row based on the duplication.
     */
    async duplicateSelected(ev, addRow = false) {
        this.copySelected();
        await this.pasteSelected();
    }

    /**
     * Adds rows at a specified index without clearing existing data.
     * @param {Array<object>} rows Array of row data to add.
     * @param {number} index The index at which to insert the new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of added ListViewItem instances.
     */
    addRowsNo(rows, index = 0) {
        let ok, err;
        let promise = new Promise((a, b) => { ok = a; err = b; });
        this.dispatchCustomEvent(this.meta.Events, customEventType.beforeCreated, rows, this).then(() => {
            const tasks = rows.map((data, i) => this.addRow(data, index + i + 1, false));
            Promise.all(tasks).then(results => {
                this.addNewEmptyRow();
                this.renderIndex();
                this.clearSelected();
                results.forEach(x => x.Selected = true);
                ok(results);
                this.dispatchCustomEvent(this.meta.Events, customEventType.afterCreated, rows).then();
            }).catch(err);
        });
        return promise;
    }

    /**
     * Updates pagination details based on the current data state.
     */
    renderIndex() {
        if (this.mainSection.Children.length === 0 || (this.meta.virtualScroll && this.isMobile())) {
            return;
        }
        this.allListViewItem.forEach((row, rowIndex) => {
            if (row.Children.length === 0 || row.firstChild === null || row.firstChild.element === null) {
                return;
            }

            for (let i = 0; i < row.element.children.length; i++) {
                const element = row.element.children[i];
                element.dataset.row = rowIndex;
                element.dataset.col = i;

                if (!this.Matrix[rowIndex]) this.Matrix[rowIndex] = [];
                this.Matrix[rowIndex][i] = element;
            }
            const previous = row.firstChild.element.closest('td').previousElementSibling;
            if (previous === null) {
                return;
            }
            const index = this.paginator.Options.startIndex + rowIndex;
            if (!this.meta.isMultiple) {
                previous.innerHTML = index.toString();
                row.Selected = this.selectedIds.some(x => x == row.entity[this.idField]);
            }
            row.rowNo = index;
        });
    }

    /**
     * Updates pagination details based on the current data state.
     */
    async renderIndex2() {
        if (this.mainSection.Children.length === 0) {
            return;
        }
        for (let rowIndex = 0; rowIndex < this.mainSection.element.children.length; rowIndex++) {
            var trElement = this.mainSection.element.children[rowIndex];
            var item = this.Item.find(x => x.element == trElement);
            var tdIndex = trElement.children[0];
            if (tdIndex != null) {
                const index = this.paginator.Options.startIndex + rowIndex;
                tdIndex.innerHTML = index.toString();
                if (item != null) {
                    item.entity[this.meta.hotKey || "Order"] = index;
                    item.updateView(true, false, "Order");
                }
            }
        }
        const columns = this.Item.map(x => x.entity).map(header => {
            const dirtyPatch = [
                { Field: "Id", Value: header.Id },
                { Field: "Order", Value: header.Order },
                { Field: "featureId", Value: header.featureId }
            ];
            return {
                Changes: dirtyPatch,
                notMessage: true,
                Table: this.meta.refName,
            };
        }).filter(x => x != null);
        Client.instance.patchAsync2(columns).then();
    }

    /**
     * Handles custom events based on row changes, applying data updates and managing component state.
     * @param {object} rowData The data of the row that triggered the change.
     * @param {ListViewItem} rowSection The ListViewItem corresponding to the row.
     * @param {ObservableArgs} observableArgs Additional arguments or data relevant to the event.
     * @param {EditableComponent} [component=null] Optional component that might be affected by the row change.
     * @returns {Promise<boolean>} A promise that resolves to a boolean indicating success or failure of the event handling.
     */
    rowChangeHandler(rowData, rowSection, observableArgs, component = null) {
        const tcs = new Promise((resolve, reject) => {
            if (!rowSection.emptyRow || !this.Editable) {
                this.dispatchEvent(this.meta.Events, EventType.Change, this, rowSection, rowData).then(() => {
                    resolve(false);
                });
            } else {
                this.dispatchCustomEvent(this.meta.Events, customEventType.beforeCreated, rowData, this).then(() => {
                    this.rowData.Data.push(rowData);
                    rowSection.filterChildren(child => true).forEach(child => {
                        child.emptyRow = false;
                        child.updateView(true);
                    });
                    this.emptySection.Children.clear();
                    this.addNewEmptyRow();
                    this.dispatchCustomEvent(this.meta.Events, customEventType.afterCreated, rowData, this).then(() => {
                        resolve(true);
                    });
                });
            }
        });
        return tcs;
    }

    /**
    * Removes a row from the ListView by its identifier.
    * @param {string} id The identifier of the row to remove.
    */
    removeRowById(id) {
        const listViewItem = this.mainSection.Children.find(x => x.entityId === id);
        if (listViewItem) {
            listViewItem.Dispose();
        }
    }

    /**
     * Adds a single row to the ListView.
     * @param {object} rowData The data object representing the row.
     * @param {number} index The index at which to insert the new row.
     * @param {boolean} singleAdd Specifies whether to add the row as a single addition.
     * @returns {Promise<ListViewItem>} A promise that resolves to the ListViewItem added.
     */
    async addRow(rowData, index = 0, singleAdd = true) {
        if (singleAdd) {
            this.rowData.Data.splice(index, 0, rowData);
        }
        await this.dispatchCustomEvent(this.meta.Events, customEventType.beforeCreated, rowData, this);
        const row = this.renderRowData(this.Header, rowData, this.mainSection, index);
        await this.dispatchCustomEvent(this.meta.Events, customEventType.afterCreated, rowData);
        return row;
    }

    /**
     * Adds multiple rows to the ListView.
     * @param {Array<object>} rows An array of objects to be added as rows.
     * @param {number} index The starting index to add new rows.
     * @returns {Promise<Array<ListViewItem>>} A promise that resolves to an array of ListViewItem instances.
     */
    async addRows(rows, index = 0) {
        await this.dispatchCustomEvent(this.meta.Events, customEventType.beforeCreatedList, this, rows);
        const listItems = [];
        await this.loadMasterData(rows);
        for (let i = 0; i < rows.length; i++) {
            const row = await this.addRow(rows[i], index + i, false);
            listItems.push(row);
        }
        await this.dispatchCustomEvent(this.meta.Events, EventType.Change, this);
        await this.dispatchCustomEvent(this.meta.Events, customEventType.afterCreatedList, this, this, rows);
        this.addNewEmptyRow();
        this.renderIndex();
        return listItems;
    }

    /**
     * Clears selected rows based on provided criteria or clears all if no criteria provided.
     */
    clearSelected() {
        this.allListViewItem.forEach(x => x.Selected = false);
        /** @type {string[]} */
        this.selectedIds = [];
        this.lastListViewItem = null;
    }

    clearFocused() {
        this.allListViewItem.forEach(x => x.Focused = false);
    }

    /**
     * Updates a specific row in the ListView.
     * @param {object} rowData The data object that represents the row to update.
     * @param {boolean} force Whether to force the update regardless of the current state.
     * @param {Array<string>} fields Specific fields to update, if provided.
     */
    updateRow(rowData, force = false, fields = []) {
        const row = this.allListViewItem.find(x => x.entity === rowData);
        if (row) {
            row.updateView(force, fields);
        }
    }

    domLoaded() {
        if (!this.meta.localRender) {
            this.Header.forEach(x => x.localData = null);
        }
        this.dOMContentLoaded?.invoke();
    }
    addContentRendered = false;
    /**
     * Renders additional content after rows have been added or updated.
     */
    contentRendered() {
        this.renderIndex();
        this.domLoaded();
        if (this.Editable) {
            this.addNewEmptyRow();
        }
    }

    getItemFocus() {
        return this.allListViewItem.find(x => x.Focused);
    }

    getRealTimeSelectedRows() {
        return new Promise((resolve, reject) => {
            // @ts-ignore
            Client.instance.getByIdAsync(this.meta.refName, this.dataConn || Client.dataConn, this.selectedIds.toArray())
                .then(res => {
                    resolve(res ? res.slice() : []);
                })
                .catch(error => {
                    reject(error);
                });
        });
    }

    getRowCountByHeight(scrollTop) {
        return (scrollTop / this._rowHeight >= 0) ?
            Math.floor(scrollTop / this._rowHeight) :
            Math.ceil(scrollTop / this._rowHeight);
    }

    removeRow(row) {
        if (row === null) {
            return;
        }
        this.rowData.Data.remove(row);
        this.mainSection.firstOrDefault(x => x.entity == row)?.Dispose();
    }

    calcTextAlign(header) {
        if (header.textAlign && header.textAlign.length > 0) {
            const parsed = Object.values(header.textAlign).includes(header.textAlign);
            if (parsed) {
                header.textAlignEnum = header.textAlign;
            }
        }
        return header;
    }

    validateAsync() {
        this.Children.forEach(x => x.validateAsync());
        if (this.validationRules.length == 0) {
            return Promise.resolve(true);
        }
        const tcs = new Promise((resolve, reject) => {
            resolve(this.validateRequired(this.value));
        });
        return tcs;
    }

    mergeComponent(sysSetting, userSetting) {
        if (!userSetting) return sysSetting;
        const column = JSON.parse(userSetting.value);
        if (!column || column.length === 0) {
            return sysSetting;
        }
        const userSettings = column.reduce((acc, current) => {
            acc[current.Id] = current;
            return acc;
        }, {});

        sysSetting.forEach(component => {
            const current = userSettings[component.id];
            if (current) {
                component.width = current.width;
                component.maxWidth = current.maxWidth;
                component.minWidth = current.minWidth;
                component.order = current.order;
                component.frozen = current.frozen;
            }
        });
        return sysSetting;
    }

    actionFilter() {
        this.clearRowData();
        this.reloadData().then();
    }

    moveUp() {
        var selected = this.getRowSelected();
        this.clearSelected();
        this.clearFocused();
        var height = 26;
        var firstElement = this.allListViewItem[0];
        if (firstElement) {
            height = firstElement.element.clientHeight;
        }
        if (!selected) {
            if (this.allListViewItem[this.allListViewItem.length - 1]) {
                this.allListViewItem[this.allListViewItem.length - 1].Selected = true;
                this.allListViewItem[this.allListViewItem.length - 1].Focused = true;
                this.dataTable.parentElement.scrollTop = this.dataTable.parentElement.scrollHeight;
                this.selectedIndex = this.allListViewItem.length - 1;
                this.Ele
            }
        }
        else {
            var indexCurrent = this.allListViewItem.indexOf(selected);
            if (!this.allListViewItem[indexCurrent - 1]) {
                this.allListViewItem[this.allListViewItem.length - 1].Selected = true;
                this.allListViewItem[this.allListViewItem.length - 1].Focused = true;
                this.dataTable.parentElement.scrollTop = this.dataTable.parentElement.scrollHeight;
                this.selectedIndex = this.allListViewItem.length - 1;
            }
            else {
                this.allListViewItem[indexCurrent - 1].Selected = true;
                this.allListViewItem[indexCurrent - 1].Focused = true;
                this.dataTable.parentElement.scrollTop = this.dataTable.parentElement.scrollTop - height;
                this.selectedIndex = indexCurrent - 1;
            }
        }
    }

    moveDown() {
        var selected = this.getRowSelected();
        this.clearSelected();
        this.clearFocused();
        var height = 26;
        var firstElement = this.allListViewItem[0];
        if (firstElement) {
            height = firstElement.element.clientHeight;
        }
        if (!selected) {
            if (this.allListViewItem[0]) {
                this.allListViewItem[0].Selected = true;
                this.allListViewItem[0].Focused = true;
                this.dataTable.parentElement.scrollTop = 0;
                this.selectedIndex = 0;
            }
        }
        else {
            var indexCurrent = this.allListViewItem.indexOf(selected);
            if (!this.allListViewItem[indexCurrent + 1]) {
                this.allListViewItem[0].Selected = true;
                this.allListViewItem[0].Focused = true;
                this.dataTable.parentElement.scrollTop = 0;
                this.selectedIndex = 0;
            }
            else {
                this.allListViewItem[indexCurrent + 1].Selected = true;
                this.allListViewItem[indexCurrent + 1].Focused = true;
                this.dataTable.parentElement.scrollTop = this.dataTable.parentElement.scrollTop + height;
                this.selectedIndex = indexCurrent + 1;
            }
        }
    }

    getUserSetting(prefix) {
        // @ts-ignore
        return Client.instance.userSvc({
            metaConn: this.metaConn,
            dataConn: this.dataConn,
            comId: "UserSetting",
            Action: "getByComId",
            Params: JSON.stringify({ comId: this.meta.Id, Prefix: prefix })
        });
    }

    /**
     * Updates a specific row in the ListView.
     * @param {ListViewItem} rowData The data object that represents the row to update.
     */
    async realtimeUpdateAsync(rowData, arg) {
        if (this.emptyRow) {
            this.emptyRow = false;
            return;
        }
        if (!this.meta.isRealtime || !arg) {
            return;
        }
        var isValid = await rowData.validateAsync();
        if (!isValid) {
            return;
        }
        if (this.editForm.childCom.some(x => !x.isListView && x.Dirty)) {
            this.editForm.savePatch().then(async () => {
                await rowData.patchUpdateOrCreate();
            });
        }
        else {
            await rowData.patchUpdateOrCreate();
        }
    }
}

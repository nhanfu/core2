import { Client } from "./clients/client.js";
import { ListView } from "./listView.js";
import { PopupEditor } from "./popupEditor.js";
import { Toast } from "./toast.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";

export class ExportCustomData extends PopupEditor {
    static Prefix = "Export";

    /**
     * @param {ListView} parentListView
     */
    constructor(parentListView) {
        super('Component');
        this.Name = "Export customData";
        this.Title = "Xuất excel tùy chọn";
        document.addEventListener('dOMContentLoaded', () => {
            this.localRender();
        });
        this.parentListView = parentListView;
        this._tbody = null;
        this._headers = [];
        this._userSetting = null;
        this._table = null;
        this._hasLoadSetting = false;
    }

    Move() {
        const self = this;
        const table = this._table;

        let draggingEle;
        let draggingRowIndex;
        let placeholder;
        let list;
        let isDraggingStarted = false;

        let x = 0;
        let y = 0;

        const swap = function (nodeA, nodeB) {
            const parentA = nodeA.parentNode;
            const siblingA = nodeA.nextSibling === nodeB ? nodeA : nodeA.nextSibling;

            nodeB.parentNode.insertBefore(nodeA, nodeB);
            parentA.insertBefore(nodeB, siblingA);
        };

        const isAbove = function (nodeA, nodeB) {
            const rectA = nodeA.getBoundingClientRect();
            const rectB = nodeB.getBoundingClientRect();

            return rectA.top + rectA.height / 2 < rectB.top + rectB.height / 2;
        };

        const cloneTable = function () {
            const rect = table.getBoundingClientRect();
            const width = parseInt(window.getComputedStyle(table).width);

            list = document.createElement('div');
            list.classList.add('clone-list');
            list.style.position = 'absolute';
            table.parentNode.insertBefore(list, table);

            table.style.visibility = 'hidden';

            table.querySelectorAll('tr').forEach(function (row) {
                const item = document.createElement('div');
                item.classList.add('draggable');

                const newTable = document.createElement('table');
                newTable.setAttribute('class', 'clone-table');
                newTable.style.width = `${width}px`;

                const newRow = document.createElement('tr');
                const cells = Array.from(row.children);
                cells.forEach(function (cell) {
                    if (cell instanceof HTMLElement) {
                        const newCell = cell.cloneNode(true);
                        if (newCell instanceof HTMLElement) {
                            newCell.style.width = `${parseInt(window.getComputedStyle(cell).width)}px`;
                            newRow.appendChild(newCell);
                        }
                    }
                });

                newTable.appendChild(newRow);
                item.appendChild(newTable);
                list.appendChild(item);
            });
        };

        const mouseDownHandler = function (e) {
            const originalRow = e.target.parentNode;
            draggingRowIndex = Array.from(table.querySelectorAll('tr')).indexOf(originalRow);

            x = e.clientX;
            y = e.clientY;

            document.addEventListener('mousemove', mouseMoveHandler);
            document.addEventListener('mouseup', mouseUpHandler);
        };

        const mouseMoveHandler = function (e) {
            if (!isDraggingStarted) {
                isDraggingStarted = true;

                cloneTable();

                draggingEle = Array.from(list.children)[draggingRowIndex];
                draggingEle.classList.add('dragging');

                placeholder = document.createElement('div');
                placeholder.classList.add('placeholder');
                draggingEle.parentNode.insertBefore(placeholder, draggingEle.nextSibling);
                placeholder.style.height = `${draggingEle.offsetHeight}px`;
            }

            draggingEle.style.position = 'absolute';
            draggingEle.style.top = `${draggingEle.offsetTop + e.clientY - y}px`;
            draggingEle.style.left = `${draggingEle.offsetLeft + e.clientX - x}px`;

            x = e.clientX;
            y = e.clientY;

            const prevEle = draggingEle.previousElementSibling;
            const nextEle = placeholder.nextElementSibling;

            if (prevEle && prevEle.previousElementSibling && isAbove(draggingEle, prevEle)) {
                swap(placeholder, draggingEle);
                swap(placeholder, prevEle);
            }

            if (nextEle && isAbove(nextEle, draggingEle)) {
                swap(nextEle, placeholder);
                swap(nextEle, draggingEle);
            }
        };

        const mouseUpHandler = function () {
            if (placeholder) {
                placeholder.parentNode.removeChild(placeholder);
            }

            draggingEle.classList.remove('dragging');
            draggingEle.style.removeProperty('top');
            draggingEle.style.removeProperty('left');
            draggingEle.style.removeProperty('position');

            const endRowIndex = Array.from(list.children).indexOf(draggingEle);

            isDraggingStarted = false;

            list.parentNode.removeChild(list);

            let rows = Array.from(table.querySelectorAll('tr'));
            if (draggingRowIndex > endRowIndex) {
                rows[endRowIndex].parentNode.insertBefore(rows[draggingRowIndex], rows[endRowIndex]);
            } else {
                rows[endRowIndex].parentNode.insertBefore(rows[draggingRowIndex], rows[endRowIndex].nextSibling);
            }

            table.style.removeProperty('visibility');

            document.removeEventListener('mousemove', mouseMoveHandler);
            document.removeEventListener('mouseup', mouseUpHandler);
            self.orderBy();
        };

        table.querySelectorAll('tr').forEach(function (row, index) {
            if (index === 0) {
                return;
            }
            const firstCell = row.firstElementChild;
            firstCell.classList.add('draggable');
            firstCell.addEventListener('mousedown', mouseDownHandler);
        });
    }


    localRender() {
        if (this.parentListView instanceof ListView) {
            this.parentListView.getUserSetting(ExportCustomData.Prefix).then(x => this.userSettingLoaded(x, true));
        }
    }

    userSettingLoaded(res, render = true) {
        this._hasLoadSetting = true;
        this._userSetting = res[0].length > 0 ? res[0][0] : null;
        if (this._userSetting) {
            let usrHeaders = JSON.parse(this._userSetting.value)
                .reduce((acc, x) => {
                    acc[x.id] = x;
                    return acc;
                }, {});
            this._headers.forEach(x => {
                x.isExport = true;
                let current = usrHeaders[x.id];
                if (current) {
                    x.isExport = current.isExport;
                    x.orderExport = current.orderExport;
                }
            });
        }
        this._headers.sort((a, b) => a.orderExport - b.orderExport);
        if (!render) return;
        let content = this.findComponentByName('Content');
        content.element.classList.add('table');
        this._table = content.element;
        // Rendering of headers and setting up drag-and-drop functionality
        this.renderDetails();
        this.move();
    }

    setChecked(item, e) {
        item.isExport = e.target.checked;
        this.Dirty = true;
    }

    dirtyCheckAndCancel() {
        this.Dirty = true;
        super.dirtyCheckAndCancel();
    }

    renderDetails() {
        Html.take(this._tbody).clear();
        let i = 1;
        for (let item of this._headers) {
            Html.instance.tRow.dataAttr("id", item.id)
                .tData.dataAttr("id", item.id).style("padding:0").iText(i.toString(), this.editForm.meta.label).end
                .tData.style("padding:0").checkbox(item.isExport).event("input", (e1) => item.isExport = e1.target.checked).end.end
                .tData.style("padding:0").className("text-left").iText(item.label, this.editForm.meta.label).end
                .endOf("tr");
            i++;
        }
        this.move();
    }

    orderBy() {
        let j = 1;
        Array.from(this._tbody.children).forEach(y => {
            const header = this._headers.find(x => x.id === y.getAttribute("data-id"));
            if (header) {
                header.orderExport = j;
                j++;
            }
        });
    }

    exportAll() {
        this.export();
    }

    exportSelected() {
        if (this.parentListView.selectedIds.length === 0) {
            Toast.warning("Select at least 1 row to export");
            return;
        }
        this.export(null, null, this.parentListView.selectedIds);
    }

    export(skip = null, pageSize = null, selectedIds = null) {
        Toast.success("Đang xuất excel");
        if (this._hasLoadSetting && this.Dirty) {
            this.parentListView.updateSetting(this._userSetting, ExportCustomData.Prefix, JSON.stringify(this._headers)).then(() => {
                this.exportWithSetting(skip, pageSize, selectedIds);
            });
            return;
        }
        this.parentListView.getUserSetting(ExportCustomData.Prefix).then(x => {
            this.userSettingLoaded(x, false);
            this.exportWithSetting(skip, pageSize, selectedIds);
        });
    }

    exportWithSetting(skip, pageSize, selectedIds) {
        let sql = this.parentListView.getSql(skip, pageSize);
        sql.count = false;
        if (this._headers.some(x => x.isExport)) {
            sql.fieldName = this._headers
                .filter(x => x.isExport)
                .map(x => x.fieldText.trim() === "" ? x.fieldName : x.fieldText);
            sql.select = this._headers.some(x => x.fieldName) ? sql.fieldName.join(", ") : null;
        }
        if (selectedIds.length > 0) {
            let ids = selectedIds.join(", ");
            sql.where = `Id in (${ids})`;
        }
        sql.params = this.parentListView.meta.label || this.parentListView.meta.refName;
        sql.table = this.parentListView.meta.refName;

        let xhrWrapper = {
            value: JSON.stringify(sql),
            url: Utils.exportExcel,
            isRawString: true,
            method: "POST"
        };

        // @ts-ignore
        Client.instance.submitAsync(xhrWrapper)
            .then(path => {
                Client.download(`/excel/Download/${path}`);
                Toast.success("Xuất file thành công");
            });
    }

}

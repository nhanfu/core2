import { EditableComponent } from "./editableComponent.js";
import { Html } from "./utils/html.js";
import { Section } from "./section.js";
import { Client } from "./clients/client.js";
import { Utils } from "./utils/utils.js";

export class TabComponent extends EditableComponent {
    /** @type {hTMLSpanElement} */
    badgeElement;
    /** @type {hTMLSpanElement} */
    textElement;
    /** @type {HTMLElement} */
    _li;
    /**
     * 
     * @param {ElementType | string | null | undefined} eleType - Element type of the section
     * @param {HTMLElement | null} ele 
     */
    constructor(ui, ele = null) {
        super(ui);
    }

    get show() {
        return this._show;
    }

    set show(value) {
        if (!this._li) {
            return;
        }
        super.show = value;
        if (value) {
            this._li.classList.add("active");
            this._li.querySelector("a").classList.add("active");
            this.dispatchEvent(this.meta.events, "focusIn", this, this.entity).then();
        } else {
            this._li.classList.remove("active");
            this._li.querySelector("a").classList.remove("active");
            this.dispatchEvent(this.meta.events, "focusOut", this, this.entity).then();
        }
    }

    set badge(value) {
        if (!value || value == 0) {
            this.badgeElement.textContent = "";
            return;
        }
        this.badgeElement.textContent = value;
    }

    render() {
        Html.take(this.parent.ul).li
            .a.className("nav-link tab-default")
            .i.className(this.meta.icon ?? "").end.span
            .iHtml(this.meta.label ?? this.meta.name, this.editForm.meta.label);
        this.textElement = Html.context;
        Html.instance.end.span.className("ml-1 badge badge-warning");
        this.badgeElement = Html.context;
        this.isTabComponent = true;
        this.editForm.tabComponents.push(this);
        if (this.meta.displayBadge) {
            Html.instance.text(this.badge ?? "");
        }
        else {
            this.badgeElement.style.display = "none";
        }
        this._li = Html.context.parentElement.parentElement;
        Html.instance.end.render();
        this._li.addEventListener("click", () => {
            if (this.hasRendered) {
                this.focus();
            }
            else {
                this.focus();
                this.renderTabContent();
            }
        });
        if (this.meta.editable) {
            this.renderTabContent();
        }
        this.countBadge();
    }

    focus() {
        this.parent.children.forEach(element => {
            element.show = false;
        });
        this.show = true;
    }

    renderTabContent() {
        Html.take(this.parent.tabContent).div.className("tab-content").display(!this.meta.editable);
        this.element = Html.context;
        Section.renderSection(this, this.meta, null, this.editForm);
        this.hasRendered = true;
    }

    countBadge() {
        if (!this.meta.displayBadge || !this.meta.Components) {
            return;
        }

        window.setTimeout(async () => {
            const updateBadge = async (meta, grid) => {
                let submitEntity = Utils.isFunction(meta.preQuery, true, grid || this);
                const vm = {
                    comId: meta.id,
                    params: submitEntity ? JSON.stringify(submitEntity) : null,
                    orderBy: (!meta.orderBy ? "ds.insertedDate desc" : meta.orderBy),
                };

                const data = await Client.instance.submitAsync({
                    noQueue: true,
                    url: `/api/feature/countBadge`,
                    method: "POST",
                    jsonData: JSON.stringify(vm, this.getCircularReplacer(), 2),
                });

                this.badge = data.count?.toString();
            };

            let gridView = this.Children.flatMap(x => x.Children).filter(x => x.meta.componentType == "GridView")[0];
            if (gridView) {
                await updateBadge(gridView.meta, gridView);  // Call sequentially when a GridView is found
            } else {
                let gridView2 = this.meta.Components.filter(x => x.componentType == "GridView")[0];
                if (gridView2) {
                    await updateBadge(gridView2);  // Call sequentially when another GridView is found
                }
            }
        }, 500);
    }


    updateViewMeta() {
        Html.take(this.textElement).iHtml(this.meta.label ?? this.meta.name, this.editForm.meta.label);
    }
}
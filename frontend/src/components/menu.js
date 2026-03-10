import {
  Client,
  EditableComponent,
  Feature,
  Html,
  ComponentExt,
  ChromeTabs,
  LangSelect,
} from "../../lib";
import EventType from "../../lib/models/eventType.js";
import { ElementType } from "../../lib/models/elementType.js";
import Sortable, { Swap } from "sortablejs";
export class MenuComponent extends EditableComponent {
  currentHref;
  /**
   * Creates an instance of the MenuComponent.
   * @param {Component} meta - The UI component.
   * @param {HTMLElement} ele - The HTML element.
   */
  constructor(meta, ele) {
    super(meta, ele);
    this.currentHref = window.location.hash;
    if (this.currentHref.startsWith("#/")) {
      this.currentHref = this.getFeatureNameFromUrl().pathname;
    }
  }
  /** @type {MenuComponent} */
  static get instance() {
    this._instance = new MenuComponent();
    return this._instance;
  }
  /**
   * @returns {string | null}
   */
  getFeatureNameFromUrl() {
    let hash = window.location.hash; // Get the full hash (e.g., '#/chat-editor?Id=-00612540-0000-0000-8000-4782e9f44882')

    if (hash.startsWith("#/")) {
      hash = hash.replace("#/", ""); // Remove the leading '#/'
    }

    if (!hash.trim() || hash == undefined) {
      return null; // Return null if the hash is empty or undefined
    }

    let [pathname, queryString] = hash.split("?"); // Split the hash into pathname and query string
    let params = new URLSearchParams(queryString); // Parse the query string into a URLSearchParams object
    if (pathname.includes("/")) {
      let segments = pathname.split("/");
      pathname = segments[segments.length - 1] || segments[segments.length - 2];
    }
    return {
      pathname: pathname || null, // Pathname (e.g., 'chat-editor')
      params: Object.fromEntries(params.entries()), // Query parameters (e.g., { Id: '-00612540-0000-0000-8000-4782e9f44882' })
    };
  }

  render() {
    new Promise(() => {
      Client.instance.submitAsync({
        Url: `/api/feature/getMenu`,
        IsRawString: true,
        Method: "GET",
      }).then((features) => {
        var cloneFeature = JSON.parse(JSON.stringify(features));
        Html.take(".search-content")
          .input.type("search")
          .event(EventType.Input, (e) => {
            var actFeature = JSON.parse(JSON.stringify(features));
            if (e.target.value) {
              var newFeatures = JSON.parse(
                JSON.stringify(
                  actFeature.filter(
                    (x) =>
                      !x.inverseParent &&
                      LangSelect.Get(x.label).toLowerCase().includes(
                        e.target.value.trim().toLowerCase()
                      )
                  )
                )
              );
              newFeatures.forEach((x) => {
                x.parentId = null;
                x.inverseParent = null;
              });
              this.buildFeatureTree(newFeatures);
              this.features = this.features.filter(x => !x.parentId || (x.parent && t.isMenu));
              this.renderMenu(this.features);
            } else {
              this.buildFeatureTree(actFeature);
              this.renderMenu(this.features);
            }
          })
          .className("form-control")
          .placeHolder("Search...")
          .end.render();
        this.buildFeatureTree(cloneFeature);
        this.renderMenu(this.features);
        if (Client.token.Vendor.Icon) {
          var icon = document.querySelector("#iconweb");
          icon.href = Client.token.Vendor.Icon;
        }
      });
    });
  }

  buildFeatureTree(features) {
    const dic = features
      .filter((f) => f.isMenu)
      .reduce((acc, f) => {
        acc[f.id] = f;
        return acc;
      }, {});

    Object.values(dic).forEach((menu) => {
      if (menu.parentId && dic.hasOwnProperty(menu.parentId)) {
        const parent = dic[menu.parentId];
        if (
          parent.inverseParent === undefined ||
          parent.inverseParent === null
        ) {
          parent.inverseParent = [];
        }
        parent.inverseParent.push(menu);
      }
    });

    Object.values(dic).forEach((menu) => {
      if (menu.inverseParent) {
        menu.inverseParent.sort((a, b) => a.order - b.order);
      }
    });

    this.features = features
      .filter((f) => !f.parentId && f.isMenu)
      .sort((a, b) => a.order - b.order);
  }
  /**
   * Renders the menu using the provided features.
   * @param {Feature[]} features - The array of Feature objects.
   */
  renderMenu(features) {
    Html.take(".sidebar-content").clear().ul.render();
    if (Client.systemRole) {
      new Sortable(Html.Context, {
        animation: 500, // Animation kéo dài hơn
        ghostClass: "blue-background-class",
        handle: "i",
        swap: false, // Chỉ swap khi thả chuột
        forceFallback: true, // Dùng clone thay vì native drag
        delay: 500, // Giữ 300ms trước khi kéo (tránh nhấp nhầm)
        delayOnTouchOnly: true, // Chỉ áp dụng delay trên cảm ứng
        easing: "cubic-bezier(0.2, 0.8, 0.2, 1)", //
        onStart: function (evt) {
          let parentGroup = evt.from;
          parentGroup.children.forEach(item => {
            item.classList.add("same-group");
          });
          evt.item.classList.add("dragging");
        },
        onEnd: async function (evt) {
          let parentGroup = evt.from;
          parentGroup.children.forEach(item => {
            item.classList.remove("same-group");
          });
          evt.item.classList.remove("dragging");
          var items = [];
          evt.from.children.forEach((x, index) => {
            var id = x.getAttribute("data-id");
            var mapItem = features.find(x => x.id == id);
            mapItem.order = index;
            const dirtyPatch = [
              { Field: "Id", Value: id },
              { Field: "Order", Value: mapItem.order }
            ];
            items.push({
              changes: dirtyPatch,
              notMessage: true,
              table: "Feature",
            })
          });
          Client.instance.patchAsync2(items).then();
        }
      });
    }
    /**
     * @param {Feature[]} features
     */
    Html.Instance.forEach(
      features,
      /**
       * @param {Feature} item
       */
      (item) => {
        if (item.isGroup) {
          Html.Instance.li.className("menu-category");
          Html.Instance.event(EventType.ContextMenu, (e) =>
            this.menuItemContextMenu(e, item)
          );
          Html.Instance.span.iText(
            item.label,
            "Menu"
          ).end.end.render();
        } else {
          var check = item.inverseParent && item.inverseParent.length > 0;
          Html.Instance.li.dataAttr("id", item.id).render();
          Html.Instance.event(EventType.ContextMenu, (e) =>
            this.menuItemContextMenu(e, item)
          );
          if (item.name == this.currentHref) {
            Html.Instance.className("active");
          }
          if (check) {
            if (item.inverseParent.some((x) => x.name == this.currentHref)) {
              Html.Instance.className("open");
              Html.Instance.className("active");
            }
          }
          Html.Instance.a.dataAttr("page", item.name).className(
            check ? "main-menu has-dropdown" : "link"
          );
          Html.Instance.event(EventType.Click, (e) =>
            this.menuItemClick(e, item)
          )
            .i.className(item.icon)
            .end.span.iText(item.label, "Menu")
            .end.render();
          Html.Instance.endOf(ElementType.a);
          if (check) {
            this.renderMenuItems(item.inverseParent);
          }
          Html.Instance.end.render();
        }
      }
    );
  }
  /**
   * @param {Feature[]} menuItems
   */
  renderMenuItems(menuItems) {
    Html.Instance.ul.render();
    if (Client.systemRole) {
      var seft = this;
      new Sortable(Html.Context, {
        animation: 500, // Animation kéo dài hơn
        ghostClass: "blue-background-class",
        handle: "i",
        swap: false, // Chỉ swap khi thả chuột
        forceFallback: true, // Dùng clone thay vì native drag
        delay: 300, // Giữ 300ms trước khi kéo (tránh nhấp nhầm)
        delayOnTouchOnly: true, // Chỉ áp dụng delay trên cảm ứng
        easing: "cubic-bezier(0.2, 0.8, 0.2, 1)",
        onStart: function (evt) {
          let parentGroup = evt.from;
          parentGroup.children.forEach(item => {
            item.classList.add("same-group");
          });
          evt.item.classList.add("dragging");
        },
        onEnd: async function (evt) {
          let parentGroup = evt.from;
          parentGroup.children.forEach(item => {
            item.classList.remove("same-group");
          });
          evt.item.classList.remove("dragging");
          var items = [];
          evt.from.children.forEach((x, index) => {
            var id = x.getAttribute("data-id");
            var mapItem = menuItems.find(x => x.id == id);
            mapItem.order = index;
            const dirtyPatch = [
              { Field: "Id", Value: id },
              { Field: "Order", Value: mapItem.order }
            ];
            items.push({
              changes: dirtyPatch,
              notMessage: true,
              table: "Feature",
            })
          });
          Client.instance.patchAsync2(items).then();
        }
      });
    }
    Html.className("sub-menu")
      .style(`max-height: ${menuItems.length * 44}px;`)
      .forEach(
        menuItems,
        /**
         * @param {Feature} item
         */
        (item) => {
          var check =
            item.inverseParent != null && item.inverseParent.count > 0;
          Html.Instance.li.dataAttr("id", item.id).render();
          Html.Instance.event(EventType.ContextMenu, (e) =>
            this.menuItemContextMenu(e, item)
          );
          if (!check) {
            if (this.currentHref == item.name) {
              Html.Instance.className("active");
            }
          }
          if (check) {
            if (item.inverseParent.some((x) => x.name == this.currentHref)) {
              Html.Instance.className("open");
              Html.Instance.className("active");
            }
          }
          Html.Instance.a.dataAttr("page", item.name).className(
            check ? "main-menu has-dropdown" : "link"
          );
          Html.Instance.event(EventType.Click, (e) =>
            this.menuItemClick(e, item)
          )
            .i.className(item.icon)
            .end.span.iText(item.label, "Menu")
            .end.render();
          Html.Instance.endOf(ElementType.a);
          if (check) {
            this.renderMenuItems(item.inverseParent);
          }
          Html.Instance.end.render();
        }
      );
    Html.Instance.endOf(ElementType.ul);
  }
  /**
   * @param {Event} e
   * @param {Feature} feature
   */
  menuItemClick(e, feature) {
    /**
     * @type {HTMLElement}
     */
    e.preventDefault();
    e.stopPropagation();
    var a = e.target;
    if (!(a instanceof HTMLAnchorElement)) {
      a = a.closest("a");
    }
    /**
     * @type {HTMLElement}
     */
    var li = a.closest(ElementType.li);
    this.hideAll(a.closest("ul"), li);
    li.classList.add("active");
    if (feature.inverseParent) {
      li.classList.toggle("open");
      var nestedUl = li.querySelector("ul");
      nestedUl.style.maxHeight = 44 * feature.inverseParent.length + "px";
      return;
    }
    var tab = ChromeTabs.tabs.find(
      (x) => x.content && x.content.meta.name == feature.name
    );
    if (tab) {
      tab.content.focus();
      return;
    }
    ComponentExt.InitFeatureByName(feature.name, true).then();
  }
  /**
   * @param {Event} e
   * @param {Feature} feature
   */
  menuItemContextMenu(e, feature) {
    e.preventDefault();
    e.stopPropagation();
  }
  /**
   * @param {HTMLElement} current
   */
  hideAll(current, ele) {
    if (!current) {
      current = document.body;
    }
    var activea = current.querySelectorAll("li.active");
    activea.forEach((x) => {
      if (x != ele) {
        x.classList.remove("active");
        x.classList.remove("open");
      }
    });
  }
}

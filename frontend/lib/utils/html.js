import { Client } from '../clients/client.js';
import { Str } from './ext.js';
import { LangSelect } from './langSelect.js';
import { PositionEnum, ElementType } from '../models/';

export class HtmlEvent {
    static click = 'click';
}
export const Direction =
{
    top: 'top',
    right: 'right',
    bottom: 'bottom',
    left: 'left',
}

export class HTML {
    /** @type {HTMLElement} */
    Context;

    /** @type {HTML} */
    get Instance() {
        return this; // This method is for backward compatibility
    }
    /**
     * 
     * @param {HTMLElement|string|null|undefined} ele 
     * @returns 
     */
    take(ele) {
        if (ele == null) return this;
        if (typeof (ele) === 'string') this.Context = document.querySelector(ele);
        else this.Context = ele;
        return this;
    }

    getContext() {
        return this.Context;
    }

    /**
     * @param {string} node
     */
    add(node) {
        const ele = document.createElement(node);
        if (this.Context) {
            this.Context.appendChild(ele);
            this.Context = ele;
        } else {
            this.Context = ele;
        }
        return this;
    }
    get div() {
        return this.add('div');
    }
    get iFrame() {
        return this.add('iframe');
    }
    get link() {
        return this.add('link');
    }
    get script() {
        return this.add('script');
    }
    get header() {
        return this.add('header');
    }
    get section() {
        return this.add('section');
    }
    get canvas() {
        return this.add('canvas');
    }
    get video() {
        return this.add('video');
    }
    get audio() {
        return this.add('audio');
    }
    get h1() {
        return this.add('h1');
    }
    get h2() {
        return this.add('h2');
    }
    get h3() {
        return this.add('h3');
    }
    get h4() {
        return this.add('h4');
    }
    get h5() {
        return this.add('h5');
    }
    get h6() {
        return this.add('h6');
    }
    get nav() {
        return this.add('nav');
    }
    get input() {
        return this.add('input');
    }
    get select() {
        return this.add('select');
    }
    get option() {
        return this.add('option');
    }
    get span() {
        return this.add('span');
    }
    get small() {
        return this.add('small');
    }
    get i() {
        return this.add('i');
    }
    get img() {
        return this.add('img');
    }
    get button() {
        return this.add('button');
    }
    get table() {
        return this.add('table');
    }
    get tHead() {
        return this.add('thead');
    }
    get th() {
        return this.add('th');
    }
    get tBody() {
        return this.add('tbody');
    }
    get tFooter() {
        return this.add('tfoot');
    }
    get tRow() {
        return this.add('tr');
    }
    get tData() {
        return this.add('td');
    }
    get p() {
        return this.add('p');
    }
    get textArea() {
        return this.add('textarea');
    }
    get details() {
        return this.add('details');
    }
    get summary() {
        return this.add('summary');
    }
    get br() {
        var br = document.createElement("br");
        this.Context.appendChild(br);
        return this;
    }
    get hr() {
        var hr = document.createElement("hr");
        this.Context.appendChild(hr);
        return this;
    }
    get ul() {
        return this.add('ul');
    }
    get li() {
        return this.add('li');
    }
    get aside() {
        return this.add('aside');
    }
    get a() {
        return this.add('a');
    }
    get form() {
        return this.add('form');
    }
    get label() {
        return this.add('label');
    }
    get end() {
        this.Context = this.Context.parentElement;
        return this;
    }
    render() {
        // Not to do anything here
    }
    /**
     * @param {string} name
     * @param {(...args) => any} handler
     * @param {any[]} args
     */
    event(name, handler, ...args) {
        this.Context.addEventListener(name, (e) => handler(e, ...args));
        return this;
    }
    /**
     * @param {string} type
     */
    trigger(type) {
        var e = new Event(type);
        this.Context.dispatchEvent(e);
        return this;
    }

    /**
     * @param {string} cls
     */
    className(cls) {
        if (this.Context.className != "") {
            this.Context.className += (' ' + cls);
        }
        else {
            this.Context.className = cls;
        }
        return this;
    }

    /**
     * @param {string} id
     */
    id(id) {
        this.Context.id = id;
        return this;
    }

    /**
     * @param {string} style
     */
    style(style) {
        if (style == null) return this;
        this.Context.style.cssText += style;
        return this;
    }
    /**
     * @param {string} width
     */
    width(width) {
        this.Context.style.width = width;
        return this;
    }

    /**
     * @param {string} direction
     * @param {number} number
     * @param {string} [unit]
     */
    padding(direction, number, unit) {
        if (unit == null) unit = 'px';
        return this.style(`padding-${direction}: ${number}${unit}`);
    }
    /**
     * @param {string} alignment
     */
    textAlign(alignment) {
        return this.style("text-align: " + alignment);
    }
    /**
     * @param {string} text
     */
    text(text) {
        if (text === null || text === undefined) return this;
        var node = new Text(text);
        this.Context.appendChild(node);
        return this;
    }

    button2(text = '', className = 'button info small', icon = '') {
        this.button.render();
        if (icon !== '') {
            this.span.className(icon).end.text(' ').render();
        }
        return this.className(className).iText(text);
    }

    /**
     * @param {string} langKey
     */
    title(langKey) {
        if (!langKey) {
            return this;
        }
        this.markLangProp(this.Context, langKey, "title");
        return this.attr("title", LangSelect.Get(langKey));
    }

    /**
     * @param {any} direction
     * @param {any} margin
     */
    margin(direction, margin, unit = "px") {
        return this.style(`margin-${direction} : ${margin}${unit}`);
    }

    /**
     * @param {string} direction
     * @param {number} margin
     */
    marginRem(direction, margin) {
        return this.style(`margin-${direction} : ${margin}rem`);
    }

    /**
     * Inserts a text node into the current HTML context with language-specific translation.
     * @param {string} langKey - The key used to fetch the translated text.
     * @param {...any} parameters - Parameters used for string formatting in the translated text.
     * @returns {Html} Returns the Html instance for chaining.
     */
    iText(langKey, featureId, ...parameters) {
        if (!langKey) {
            return this;
        }
        const translated = LangSelect.Get(langKey, featureId);
        const textContent = parameters.length > 0 ? Str.Format(translated, parameters) : translated;
        const textNode = document.createTextNode(textContent);
        this.markLangProp(textNode, langKey, "textContent", parameters);
        textNode["featurename"] = featureId;
        this.Context.appendChild(textNode);
        return this;
    }

    /**
     * @param {string} html
     */
    innerHTML(html) {
        this.Context.innerHTML = html;
        return this;
    }
    /**
     * @param {boolean} val
     */
    smallCheckbox(val = false, disabled = false) {
        var attr = disabled ? "disabled" : "enabled";
        this.label.className("checkbox input-small transition-on style2")
            .input.attr(attr, attr).attr("type", "checkbox").type("checkbox").end
            .span.className("check myCheckbox");
        // @ts-ignore
        this.Context.previousElementSibling.checked = val;
        return this;
    }
    /**
     * @param {string} name
     */
    type(name) {
        // @ts-ignore
        this.Context.type = name;
        return this;
    }
    /**
     * @param {string} name
     * @param {string} value
     */
    attr(name, value) {
        this.Context.setAttribute(name, value);
        return this;
    }

    href(value) {
        this.Context.setAttribute("href", value);
        return this;
    }

    src(value) {
        this.Context.setAttribute("src", value);
        return this;
    }

    /**
     * @param {number} index
     */
    tabIndex(index) {
        this.Context.setAttribute('tabindex', index.toString());
        return this;
    }

    /**
     * @param {string} name
     * @param {string} value
     */
    dataAttr(name, value) {
        this.Context.setAttribute('data-' + name, value);
        return this;
    }

    /**
     * @param {string} langKey
     */
    placeHolder(langKey) {
        if (!langKey || langKey.trim() === '') {
            return this;
        }
        this.markLangProp(this.Context, langKey, "placeholder");
        return this.attr("placeholder", LangSelect.Get(langKey));
    }


    /**
     * Marks a language property on a specified node.
     * @param {Node} ctx - The context node to which the language properties are added.
     * @param {string} langKey - The key of the language property.
     * @param {string} propName - The name of the property to mark.
     * @param {...any} parameters - Additional parameters associated with the language property.
     */
    markLangProp(ctx, langKey, propName, ...parameters) {
        if (!ctx) return;

        const langKeyProperty = LangSelect.LangKey + propName;
        const langParamProperty = LangSelect.LangParam + propName;

        ctx[langKeyProperty] = langKey;
        if (parameters.length > 0) {
            ctx[langParamProperty] = parameters;
        }

        const prop = ctx[LangSelect.LangProp];
        const newProp = prop ? prop + "," + propName : propName;
        const propArray = newProp.split(",").filter((value, index, self) => self.indexOf(value) === index);
        ctx[LangSelect.LangProp] = propArray.join(",");
    }
    /**
     * @param {string} val
     */
    value(val) {
        /** @type {HTMLInputElement} */
        // @ts-ignore
        const input = this.Context;
        input.value = val;
        return this;
    }
    /**
     * Adds an icon to the HTML element.
     * @param {string} icon - Icon class or URL to set as background.
     * @returns {Html} Returns this for chaining.
     */
    icon(icon) {
        const isIconClass = icon && (icon.includes("mif") || icon.includes("fa") || icon.includes("fa-"));
        this.span.className("icon");
        if (isIconClass) {
            this.className(icon).render();
        } else {
            this.style(`background-image: url(${Client.Origin + icon});`).className("iconBg").render();
        }
        return this;
    }
    /**
     * @param {PositionEnum | string} position
     * @param {string | number} distance
     * @param {string} unit
     */
    position(position, distance = null, unit = 'px') {
        if (distance == null)
            return this.style(`position: {${position}}`);
        return this.style(`position: ${position}; ${position}: ${distance}${unit};`);
    }


    /**
     * Sets up an escape key event listener on the current context.
     * @param {Function} action - Action to execute when the escape key is pressed.
     * @returns {Html} Returns this for chaining.
     */
    escape(action) {
        const div = this.Context;
        div.tabIndex = -1;
        div.focus();
        div.addEventListener('keydown', (e) => {
            if (e.keyCode === 27) { // Escape key
                const parent = div.parentElement;
                e.stopPropagation();
                action(e);
                parent.focus();
            }
        });
        return this;
    }
    /**
     * Sets an icon for a span element.
     * @param {string} iconClass - Icon class or URL to set as background.
     * @returns {Html} Returns this for chaining.
     */
    iconForSpan(iconClass) {
        if (!iconClass || iconClass.trim().length === 0) {
            return this;
        }
        iconClass = iconClass.trim();
        const span = this.Context;
        this.className("icon");
        const isIconClass = iconClass.includes("mif") || iconClass.includes("fa") || iconClass.includes("fa-");
        if (isIconClass) {
            span.classList.add(iconClass);
        } else {
            span.classList.add("iconBg");
            span.style.backgroundImage = `url(${iconClass})`;
        }
        return this;
    }
    /**
     * Sets the position of the current HTML element to fixed.
     * @param {string} top - The top position in pixels.
     * @param {string} left - The left position in pixels.
     * @returns {Html} Returns this for chaining.
     */
    floating(top, left) {
        return this.position(PositionEnum.fixed)
            .position(Direction.top, top)
            .position(Direction.left, left);
    }
    /**
     * Inserts HTML content into the current context with optional language translation.
     * @param {string} langKey - Language key for translation.
     * @param {...any} parameters - Parameters for formatting the translation.
     * @returns {Html} Returns this for chaining.
     */
    iHtml(langKey, featureId, ...parameters) {
        if (!langKey) {
            return this;
        }
        const ctx = this.Context;
        const translated = LangSelect.Get(langKey, featureId);
        this.markLangProp(ctx, langKey, 'innerHTML', parameters);
        ctx.innerHTML = translated;
        return this;
    }
    /**
     * Sets the colspan attribute for an HTML table cell.
     * @param {number} colSpan - The number of columns to span.
     * @returns {Html} Returns this for chaining.
     */
    colSpan(colSpan) {
        return this.attr("colspan", colSpan.toString());
    }

    /**
     * Sets the rowspan attribute for an HTML table cell.
     * @param {number} rowSpan - The number of rows to span.
     * @returns {Html} Returns this for chaining.
     */
    rowSpan(rowSpan) {
        return this.attr("rowspan", rowSpan.toString());
    }

    /**
     * Ends the current context at the specified element type or selector.
     * @param {string|ElementType} selector - The selector or element type to end at.
     * @returns {Html} Returns this for chaining.
     */
    endOf(selector) {
        if (typeof selector === "object" && selector.toString) { // Assuming ElementType is an object with toString()
            selector = selector.toString();
        }

        let result = this.Context;
        while (result !== null) {
            // @ts-ignore
            if (result.querySelector(selector) !== null) {
                break;
            } else {
                result = result.parentElement;
            }
        }

        if (result === null) {
            throw new Error("Cannot find the element of selector " + selector);
        }

        this.Context = result;
        return this;
    }

    /**
     * Moves the context to the closest ancestor that matches the specified element type.
     * @param {ElementType} type - The element type to find the closest ancestor.
     * @returns {Html} Returns this for chaining.
     */
    closest(type) {
        if (this.Context && typeof this.Context.closest === 'function') {
            this.Context = this.Context.closest(type.toString());
        }
        return this;
    }

    clear() {
        this.Context.innerHTML = '';
        return this;
    }

    checkbox(value) {
        this.add(ElementType.input);
        var checkbox = this.Context;
        if (checkbox instanceof HTMLInputElement) {
            checkbox.setAttribute("type", "checkbox");
            checkbox.checked = value ?? false;
        }
        return this;
    }

    /**
     * Sets the sticky position to the HTML context.
     * @param {string} [top=null] - Set top to '0px' if it's aligned top with previous element.
     * @param {string} [left=null] - Set left to '0px' if it's aligned left with previous element.
     * @returns {HTML} Returns the instance of the class for chaining.
     */
    sticky(top = null, left = null) {
        const ctx = this.Context;
        if (!ctx) {
            return this;
        }

        if (ctx.previousElementSibling && ctx.tagName === ctx.previousElementSibling.tagName) {
            if (left === '0') {
                left = `${ctx.offsetLeft}px`;
            } else if (top === '0') {
                top = `${ctx.offsetTop}px`;
            }
        }

        if (top !== null) {
            this.style(`top: ${top};`);
        }

        if (left !== null) {
            this.style(`left: ${left};`);
        }

        return this.style("position: sticky; z-index: 1;");
    }

    forEach(array, callback) {
        for (let index = 0; index < array.length; index++) {
            callback(array[index], index);
        }
        return this;
    }

    display(shouldShow) {
        const ele = this.Context;
        ele.style.display = shouldShow ? '' : 'none';
        return this;
    }

    visibility(visible) {
        var ele = this.Context;
        ele.style.visibility = visible ? "" : "hidden";
        return this;
    }
}

export const Html = new HTML();
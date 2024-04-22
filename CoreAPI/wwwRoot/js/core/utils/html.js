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
    _instance;
    /** @type {HTML} */
    get Instance() {
        if (this._instance == null) this._instance = new HTML();
        return this._instance;
    }
    Take(ele) {
        if (ele == null) return;
        if (typeof (ele) === 'string') ele = document.querySelector(ele);
        this.Context = ele;
        return this;
    }
    Add(node) {
        const ele = document.createElement(node);
        this.Context.appendChild(ele);
        this.Context = ele;
        return this;
    }
    get Div() {
        return this.Add('div');
    }
    get Iframe() {
        return this.Add('iframe');
    }
    get Link() {
        return this.Add('link');
    }
    get Script() {
        return this.Add('script');
    }
    get Header() {
        return this.Add('header');
    }
    get Section() {
        return this.Add('section');
    }
    get Canvas() {
        return this.Add('canvas');
    }
    get Video() {
        return this.Add('video');
    }
    get Audio() {
        return this.Add('audio');
    }
    get H1() {
        return this.Add('h1');
    }
    get H2() {
        return this.Add('h2');
    }
    get H3() {
        return this.Add('h3');
    }
    get H4() {
        return this.Add('h4');
    }
    get H5() {
        return this.Add('h5');
    }
    get H6() {
        return this.Add('h6');
    }
    get Nav() {
        return this.Add('nav');
    }
    get Input() {
        return this.Add('input');
    }
    get Select() {
        return this.Add('select');
    }
    get Option() {
        return this.Add('option');
    }
    get Span() {
        return this.Add('span');
    }
    get Small() {
        return this.Add('small');
    }
    get I() {
        return this.Add('i');
    }
    get Img() {
        return this.Add('img');
    }
    get Button() {
        return this.Add('button');
    }
    get Table() {
        return this.Add('table');
    }
    get Thead() {
        return this.Add('thead');
    }
    get Th() {
        return this.Add('th');
    }
    get TBody() {
        return this.Add('tbody');
    }
    get TFooter() {
        return this.Add('tfoot');
    }
    get TRow() {
        return this.Add('tr');
    }
    get TData() {
        return this.Add('td');
    }
    get P() {
        return this.Add('p');
    }
    get TextArea() {
        return this.Add('textarea');
    }
    get Br() {
        var br = document.createElement("br");
        this.Context.appendChild(br);
        return this;
    }
    get Hr() {
        var hr = document.createElement("hr");
        this.Context.appendChild(hr);
        return this;
    }
    get Ul() {
        return this.Add('ul');
    }
    get Li() {
        return this.Add('li');
    }
    get Aside() {
        return this.Add('aside');
    }
    get A() {
        return this.Add('a');
    }
    get Form() {
        return this.Add('form');
    }
    get Label() {
        return this.Add('label');
    }
    get End() {
        this.Context = this.Context.parentElement;
        return this;
    }
    Render() {
        // Not to do anything here
    }
    Event(name, handler) {
        this.Context.addEventListener(name, handler);
        return this;
    }
    ClassName(cls) {
        this.Context.className += (' ' + cls);
        return this;
    }
    Style(style) {
        if (style == null) return this;
        this.Context.style.cssText += style;
        return this;
    }
    Padding(direction, number, unit) {
        if (unit == null) unit = 'px';
        return this.Style(`padding-${direction}: ${number}${unit}`);
    }
    TextAlign(alignment) {
        return this.Style("text-align: " + alignment);
    }
    Text(text) {
        if (text === null || text === undefined) return this;
        var node = new Text(text);
        this.Context.appendChild(node);
    }
    InnerHTML(html) {
        this.Context.innerHTML = html;
        return this;
    }
    SmallCheckbox(val) {
        this.Label.ClassName("checkbox input-small transition-on style2")
            .Input.Attr("type", "checkbox").Type("checkbox").End
            .Span.ClassName("check myCheckbox");
        this.Context.PreviousElementSibling.checked = val;
        return this;
    }
    Attr(name, value) {
        this.Context.setAttribute(name, value);
        return this;
    }
    DataAttr(name, value) {
        this.Context.setAttribute('data-'+name, value);
        return this;
    }
}

export const Html = new HTML();
import EditableComponent from "./editableComponent.js";
import { Component } from "./models/component.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import EventType from "./models/eventType.js";
import { string } from "./utils/ext.js";

/**
 * Represents a button component that can be rendered and managed on a web page.
 */
export class DateTimePicker extends EditableComponent {
    /**
     * Creates an instance of the Button component.
     * @param {Component} ui - The UI component metadata.
     * @param {HTMLElement} [ele=null] - The HTMLElement to be used as the button element.
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        if (!this.ParentElement) {
            this.ParentElement = ele;
        }
        if (!ui) throw new Error("ui is required");
        /** @type {Component} */
        this.Meta = ui;
        /** @type {string} */
        this.HHmmFormat = "00";
        /** @type {Date} */
        this._value = false;
        /** @type {boolean} */
        this._simpleNoEvent = false;
        /** @type {HTMLInputElement} */
        this.Input = null;
        /** @type {string} */
        this.InitFormat = "";
        /** @type {string} */
        this._currentFormat = "";
        /** @type {HTMLElement} */
        this._calendar = null;
        /** @type {number} */
        this._renderAwaiter = null;
        /** @type {number} */
        this._closeAwaiter = null;
        /** @type {boolean} */
        this.show = false;
        /** @type {Date} */
        this._someday = null;
        /** @type {HTMLInputElement} */
        this._hour = null;
        /** @type {HTMLInputElement} */
        this._minute = null;
        /** @type {string[]} */
        this.formats = ["dd/MM/yyyy - HH:mm", "dd/MM/yyyy - HH:m", "dd/MM/yyyy - h:mm",
            "dd/MM/yyyy - HH:", "dd/MM/yyyy - h:", "dd/MM/yyyy - h:m", "dd/MM/yyyy - HH",
            "dd/MM/yyyy - h", "dd/MM/yyyy", "ddMMyyyy", "d/M/yyyy", "dMyyyy", "dd/MM/yy",
            "ddMMyy", "d/M", "dM", "dd/MM", "ddMM"];
        this.InitFormat = this.Meta.FormatData != undefined ? this.Meta.FormatData.replaceAll("{0:", "").replaceAll("}", "")
            : (this.Meta.Precision == 7 ? "dd/MM/yyyy - HH:mm" : "dd/MM/yyyy");
        this._currentFormat = this.InitFormat;
    }

    /** @type {Date} */
    get Value() {
        return this._value;
    }

    /**
     * Sets the value of the component
     * @param {Date} value - The value to set
     */
    set Value(value) {
        if (this._value == value) {
            return;
        }
        this._value = value;
        if (this._value != null) {
            var selectionEnd = this.Input.SelectionEnd;
            this.Input.Value = this._value != new Date(0) ? this._value.Value.ToString(this._currentFormat) : "";
            this.Input.SelectionStart = selectionEnd;
            this.Input.SelectionEnd = selectionEnd;
        }
        else if (!_nullable) {
            this._value = new Date();
            this.Input.Value = this._value.Value.ToString(this._currentFormat);
        }
        else {
            this.Input.Value = null;
        }
        this.Entity.SetComplexPropValue(this.FieldName, this._value);
        this.Dirty = true;
    }

    /**
     * Renders the button component into the DOM.
     */
    Render() {
        this.SetDefaultVal();
        var fieldValue = Utils.GetPropValue(this.Entity, this.FieldName);
        this.Entity.SetComplexPropValue(this.FieldName, this._value);
        var parsedVal = new Date(0);
        var parsed = typeof fieldValue == 'string' && fieldValue != null && fieldValue != '';
        this._value = fieldValue == null ? null : moment(fieldValue, "YYYYMMDD")
        this.Entity.SetComplexPropValue(this.FieldName, this._value);
        var str = this._value != null && this._value != new Date(0) ? this._value.Value.ToString(InitFormat) : "";
        this.OldValue = this._value != new Date(0) ? this._value?.ToString().DateConverter() : "";
        if (this.Input == null) {
            Html.Take(this.ParentElement).Div.ClassName("datetime-picker").TabIndex(-1).Input.Render();
            this.Element = this.Input = Html.Context;
        }
        else {
            Html.Take(this.Input);
            this.Element = this.Input;
        }
        Html.Event(EventType.KeyDown, (e) => {
            if (Disabled || e == null) {
                return;
            }
            var code = e.KeyCode();
            if (code == 13) {
                ParseDate();
            }
        });
        Html.Value(str)
            .Event(EventType.Focus, () => {
                if (this._simpleNoEvent) {
                    this._simpleNoEvent = false;
                    return;
                }
                if (!this.Meta.FocusSearch) {
                    this.RenderCalendar();
                }
            })
            .Event(EventType.Change, () => this.ParseDate())
            .PlaceHolder(this.Meta.PlainText);
        this.Input.AutoComplete = "off";
        this.Input.Name = this.FieldName;
        this.Input.parentElement.addEventListener(EventType.FocusOut, this.CloseCalendar());
        this.Input.addEventListener(EventType.KeyDown, (e) => this.KeyDownDateTime(e));
        Html.End.Div.ClassName("btn-group").Button.TabIndex(-1).Span.ClassName("icon mif-calendar")
            .Event(EventType.Click, () => {
                if (this.Input.Disabled) {
                    return;
                }

                if (this.show) {
                    this.CloseCalendar();
                }
                else {
                    this.RenderCalendar();
                }
            });
        this.DOMContentLoaded?.Invoke();
    }

    RenderCalendarTask(someday = null) {
        if (this.Disabled) {
            return;
        }

        var someday = this._someday ?? this._value ?? new Date();
        this.show = true;
        window.clearTimeout(this._closeAwaiter);
        if (this._calendar != null) {
            this._calendar.InnerHTML = null;
            this._calendar.Style.Display = string.Empty;
        }
        else {
            Html.Take(document.body).Div.ClassName("calendar compact open open-up").TabIndex(-1).Trigger(EventType.Focus);
            this._calendar = Html.Context;
        }
        Html.Take(this._calendar).Div.ClassName("calendar-header")
            .Div.ClassName("header-year").Text(this._someday.getFullYear().ToString()).End
            .Div.ClassName("header-day").Text(`${this._someday}, ${this._someday.Month} ${this._someday.Day}`).End.End
            .Div.ClassName("calendar-content")
            .Div.ClassName("calendar-toolbar")
            .Span.ClassName("prev-month").Event(EventType.Click, () => {
                this._someday = this._someday.AddMonths(-1);
                this.RenderCalendar(_someday);
            }).Span.ClassName("fa fa-chevron-left").End.End
            .Span.ClassName("curr-month").Text(string.Format("{0:00}", _someday.Month)).End
            .Span.ClassName("next-month").Event(EventType.Click, () => {
                this._someday = this._someday.AddMonths(1);
                this.RenderCalendar(this._someday);
            }).Span.ClassName("fa fa-chevron-right").End.End

            .Span.ClassName("prev-year").Event(EventType.Click, () => {
                this._someday = this._someday.AddYears(-1);
                this.RenderCalendar(this._someday);
            }).Span.ClassName("fa fa-chevron-left").End.End
            .Span.ClassName("curr-year").Text(_someday.Year.ToString()).End
            .Span.ClassName("next-year").Event(EventType.Click, () => {
                this._someday = this._someday.AddYears(1);
                this.RenderCalendar(this._someday);
            }).Span.ClassName("fa fa-chevron-right").End.End
            .End
            .Div.ClassName("week-days")
            .Span.ClassName("day").IText("Mo").End
            .Span.ClassName("day").IText("Tu").End
            .Span.ClassName("day").IText("We").End
            .Span.ClassName("day").IText("Th").End
            .Span.ClassName("day").IText("Fr").End
            .Span.ClassName("day").IText("Sa").End
            .Span.ClassName("day").IText("Su").End
            .End
            .Div.ClassName("days");
        var now = new Date();
        var firstDayOfMonth = new DateTime(_someday.Year, _someday.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        var firstOutsideDayOfMonth = firstDayOfMonth.AddDays(-(firstDayOfMonth.DayOfWeek + 1) - 5);
        var lastOutsideDayOfMonth = lastDayOfMonth.AddDays(6 - (lastDayOfMonth.DayOfWeek + 1));
        if ((lastOutsideDayOfMonth - firstOutsideDayOfMonth).Days / 7 < 5) {
            lastOutsideDayOfMonth = lastOutsideDayOfMonth.AddDays(7);
        }
        var runner = firstOutsideDayOfMonth;
        while (runner <= lastOutsideDayOfMonth) {

        }
    }

    RenderCalendar(someday = null) {
        if (this.Disabled) {
            return;
        }
        window.clearTimeout(this._renderAwaiter);
        window.clearTimeout(this._closeAwaiter);
        this._renderAwaiter = window.setTimeout(() => this.RenderCalendarTask(someday), 100);
    }

    KeyDownDateTime(evt) {
        if (evt.key() == 13 && !this._value && !this.EditForm.Feature.CustomNextCell) {
            if (Input.Disabled) {
                return;
            }

            if (show) {
                CloseCalendar();
            }
            else {
                RenderCalendar();
            }
        }
    }

    CloseCalendar() {
        this._closeAwaiter = window.setTimeout(() => {
            this.show = false;
            if (this._calendar != null) {
                this._calendar.Style.Display = Display.None;
            }
            this.Input.Value = this._value?.ToString(InitFormat);
            this._hour = null;
            this._minute = null;
        }, 250);
    }

    /**
     * Dispatches the click event, handles UI changes for click action.
     */
    async DispatchClick() {
        if (this.Disabled || this.Element.hidden) {
            return;
        }
        this.Disabled = true;
        try {
            Spinner.AppendTo(this.Element);
            await this.DispatchEvent(this.Meta.Events, "click", this.Entity, this);
            this.Disabled = false;
            Spinner.Hide();
        } finally {
            window.setTimeout(() => {
                this.Disabled = false;
            }, 2000);
        }
    }

    /**
     * Gets the value text from the button component.
     * @returns {string} The text value of the component.
     */
    GetValueText() {
        if (!this.Entity || !this.FieldName) {
            return this._textEle.textContent;
        }
        return this.FieldVal?.toString();
    }
}

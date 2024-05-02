import EditableComponent from "./editableComponent.js";
import ObservableArgs from "./models/observable.js";
import { string } from "./utils/ext.js";
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";

export class Datepicker extends EditableComponent {
    static HHmmFormat = "00";
    static formats = [
        "dd/MM/yyyy - HH:mm", "dd/MM/yyyy - HH:m", "dd/MM/yyyy - h:mm",
        "dd/MM/yyyy - HH:", "dd/MM/yyyy - h:", "dd/MM/yyyy - h:m", "dd/MM/yyyy - HH", "dd/MM/yyyy - h", "dd/MM/yyyy", "ddMMyyyy", "d/M/yyyy", "dMyyyy", "dd/MM/yy", "ddMMyy", "d/M", "dM", "dd/MM", "ddMM"
    ];
    static options = { weekday: 'short', year: 'numeric', month: 'long', day: 'numeric' };
    static calendar = null;
    static renderAwaiter = null;
    static closeAwaiter = null;

    constructor(ui, ele = null) {
        super(ui);
        this.DefaultValue = new Date("0001-01-01T00:00:00Z");
        if (!ui) throw new Error("ui is required");
        /** @type {Component} */
        this.Meta = ui;
        this.InitFormat = this.Meta.FormatData?.includes("{0:") ? this.Meta.FormatData.replace("{0:", "").replace("}", "") : (this.Meta.Precision === 7 ? "dd/MM/yyyy - HH:mm" : "dd/MM/yyyy");
        this.currentFormat = this.InitFormat;
        if (ele != null) {
            if (ele.firstElementChild instanceof HTMLInputElement) {
                this.Input = ele.firstElementChild;
            } else {
                this.Input = Html.Take(ele).Input.Context;
            }
        }
        this.value = null;
        this.nullable = false;
        this.simpleNoEvent = false;
        this.show = false;
        this.someday = new Date();
        this.hour = null;
        this.minute = null;
    }

    /**
     * Gets the value of the datepicker.
     * @returns {Date | null}
     */
    get Value() {
        return this.value;
    }

    /**
     * Sets the value of the datepicker and triggers related updates.
     * @param {Date | null} value - The new date value.
     */
    set Value(value) {
        if (this.value === value) {
            return;
        }
        this.value = value;
        if (this.value) {
            const selectionEnd = this.Input.selectionEnd;
            var isFn = Utils.IsFunction(this.Meta.FormatEntity);
            if (isFn) this.Input.value = isFn.call(this, value, this);
            else {
                this.Input.value = this.value.getTime() !== new Date("0001-01-01T00:00:00Z").getTime()
                    ? this.value.toLocaleDateString('en-US', Datepicker.options) : "";
            }
            this.Input.selectionStart = selectionEnd;
            this.Input.selectionEnd = selectionEnd;
        } else if (!this.nullable) {
            this.value = new Date();
            this.Input.value = this.value.toLocaleDateString('en-US', Datepicker.options);
        } else {
            this.Input.value = "";
        }
        this.Entity.SetComplexPropValue(this.FieldName, this.value);
        this.Dirty = true;
    }

    /**
     * Renders the datepicker component in the DOM.
     */
    Render() {
        this.SetDefaultVal();
        let fieldValue = this.FieldVal;
        this.Entity.SetComplexPropValue(this.FieldName, this.value);
        let parsedVal = new Date(fieldValue);
        let parsed = fieldValue && parsedVal.toString() !== "Invalid Date";
        this.value = parsed ? parsedVal : null;
        this.nullable = this.FieldVal == null;
        this.Entity.SetComplexPropValue(this.FieldName, this.value);
        let str = this.value ? this.value.toLocaleDateString('en-US', this.options) : "";
        Html.Take(this.ParentElement);
        if (!this.Input) {
            Html.Div.ClassName("datetime-picker").TabIndex(-1);
            Html.Input.Render();
            this.Element = Html.Context;  // Assuming Html.Context provides the last rendered element
            this.Input = this.Element;
        } else {
            Html.Take(this.Input);
            this.Element = this.Input;
        }
        Html.Event("keydown", (e) => {
            if (this.Disabled || !e) return;
            if (e.keyCode === 13) this.ParseDate(); // Enter key
        }).Value(str).Event("focus", () => {
            if (this.simpleNoEvent) {
                this.simpleNoEvent = false;
                return;
            }
            if (!this.Meta.FocusSearch) {
                this.RenderCalendar();
            }
        }).Event("change", () => this.ParseDate())
            .PlaceHolder(this.Meta.PlainText).Attr("autocomplete", "off")
            .Attr('name', this.FieldName)
        this.Input.addEventListener("keydown", (e) => this.KeyDownDateTime(e));
        this.Input.parentElement?.addEventListener("focusout", () => this.CloseCalendar());
        Html.End.Div.ClassName("btn-group").Button.TabIndex(-1).Span.ClassName("fa fa-calendar")
            .Event("click", () => {
                if (this.Input.Disabled) return;
                this.show ? this.CloseCalendar() : this.RenderCalendar();
            });
    }


    /**
     * Handles key down events specifically for managing date and time inputs.
     * @param {Event} e - The event object.
     */
    KeyDownDateTime(e) {
        if (e.keyCode === 13 && this.value === null) {
            if (this.Disabled) {
                return;
            }
            if (this.show) {
                this.CloseCalendar();
            } else {
                this.RenderCalendar();
            }
        }
    }

    /**
     * Determines if a given type is nullable.
     * @param {string} type - The type to check.
     * @returns {boolean} Whether the type is nullable.
     */
    IsNullable(type) {
        return this.Entity === null || this.Utils.IsNullable(type, this.Entity.GetType(), this.FieldName, this.Entity);
    }

    /**
     * Parses the date from the input and sets the component's value.
     */
    ParseDate() {
        let { parsed, datetime } = this.TryParseDateTime(this.Input.value);
        if (!parsed || !this.Input.value.trim()) {
            if (this.EditForm.Feature.CustomNextCell) {
                return;
            }
            this.Input.value = "";
            this.TriggerUserChange(null);
        } else {
            this.Value = datetime;
            this.TriggerUserChange(datetime);
        }
    }

    /**
     * Closes the calendar UI.
     */
    CloseCalendar() {
        setTimeout(() => {
            this.show = false;
            if (this.calendar) {
                this.calendar.style.display = "none";
            }
            this.Input.value = this.value ? this.value.toLocaleDateString('en-US', this.options) : "";
            this.hour = null;
            this.minute = null;
        }, 250);
    }

    /**
     * Renders the calendar UI.
     * @param {Date} someday - The date to use for rendering the calendar.
     */
    RenderCalendar(someday = null) {
        if (this.Disabled) {
            return;
        }
        setTimeout(() => {
            this.RenderCalendarTask(someday || this.value || new Date());
        }, 100);
    }

    /**
     * Detailed rendering logic for the calendar, handling navigation and selection of dates.
     * @param {Date} someday - The date to render in the calendar.
     */
    RenderCalendarTask(someday) {
        if (this.Disabled) return;
        this.show = true;
        clearTimeout(this.closeAwaiter);
        if (!this.calendar) {
            Html.Take(document.body).Div.ClassName("calendar compact open open-up").TabIndex(-1).Trigger("focus");
            this.calendar = Html.Context; // Assumes Html.Context provides the current element
        } else {
            Html.Take(this.calendar);
            this.calendar.innerHTML = "";
            this.calendar.style.display = "block";
        }

        Html.Div.ClassName("calendar-header")
            .Div.ClassName("header-year").Text(someday.getFullYear().toString()).End
            .Div.ClassName("header-day").Text(`${someday.toLocaleDateString('en', { weekday: 'long' })}, ${someday.getMonth() + 1}/${someday.getDate()}`).End
            .End
            .Div.ClassName("calendar-content")
            .Div.ClassName("calendar-toolbar")
            .Span.ClassName("prev-month").Event("click", () => {
                this.RenderCalendar(new Date(someday.getFullYear(), someday.getMonth() - 1, someday.getDate()));
            }).Span.ClassName("fa fa-chevron-left").End.End
            .Span.ClassName("curr-month").Text((someday.getMonth() + 1).toString()).End
            .Span.ClassName("next-month").Event("click", () => {
                this.RenderCalendar(new Date(someday.getFullYear(), someday.getMonth() + 1, someday.getDate()));
            }).Span.ClassName("fa fa-chevron-right").End.End
            .Span.ClassName("prev-year").Event("click", () => {
                this.RenderCalendar(new Date(someday.getFullYear() - 1, someday.getMonth(), someday.getDate()));
            }).Span.ClassName("fa fa-chevron-left").End.End
            .Span.ClassName("curr-year").Text(someday.getFullYear().toString()).End
            .Span.ClassName("next-year").Event("click", () => {
                this.RenderCalendar(new Date(someday.getFullYear() + 1, someday.getMonth(), someday.getDate()));
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
        var firstDayOfMonth = new Date(someday.getFullYear(), someday.getMonth(), 1);
        var lastDayOfMonth = new Date(firstDayOfMonth);
        lastDayOfMonth.setMonth(lastDayOfMonth.getMonth() + 1);
        lastDayOfMonth.setDate(lastDayOfMonth.getDate() - 1);
        var firstOutsideDayOfMonth = new Date(firstDayOfMonth);
        firstOutsideDayOfMonth.setDate(firstOutsideDayOfMonth.getDate() - (firstDayOfMonth.getDay() + 1) - 5);
        var lastOutsideDayOfMonth = new Date(lastDayOfMonth);
        lastOutsideDayOfMonth.setDate(lastOutsideDayOfMonth.getDate() + (6 - lastDayOfMonth.getDay() + 1));
        if ((lastOutsideDayOfMonth - firstOutsideDayOfMonth) / (1000 * 60 * 60 * 24 * 7) < 5) {
            lastOutsideDayOfMonth.setDate(lastOutsideDayOfMonth.getDate() + 7);
        }
        var runner = new Date(firstOutsideDayOfMonth);
        const handler = this.SetSelectedDay.bind(this);
        while (runner <= lastOutsideDayOfMonth) {
            for (var day = 0; day <= 6; day++) {
                if (runner.getDay() === 1) {
                    Html.Div.ClassName("days-row");
                }
                // Assuming Html.Instance.Div.ClassName and Html.Instance.Div.Text are functions
                Html.Div.ClassName("day").Text(runner.getDate().toString())
                    .Event("click", handler, runner);
                if (runner.getDate() === now.getDate() && runner.getMonth() === now.getMonth() && now.getFullYear() === someday.getFullYear()) {
                    Html.ClassName("showed today");
                }

                if (this._value !== undefined && runner.getDate() === this._value.getDate() && runner.getMonth() === this._value.getMonth() && runner.getFullYear() === this._value.getFullYear()) {
                    Html.ClassName("selected");
                }

                if (runner < firstDayOfMonth || runner > lastDayOfMonth) {
                    Html.ClassName("outside");
                }

                Html.End.Render();
                if (runner.getDay() === 0) {
                    Html.End.Render();
                }
                runner = runner.addDays(1);
            }
        }
    }

    /**
     * Attempts to parse a datetime string using known formats.
     * @param {string} value - The datetime string to parse.
     * @returns {{parsed: boolean, datetime: Date | null}}
     */
    static TryParseDateTime(value) {
        let dateTime = Date.parse(value); // Assuming Moment.js is used
        if (isNaN(dateTime) || !isFinite(dateTime)) {
            return { parsed: true, datetime: new Date(dateTime) };
        }
        return { parsed: false, datetime: null };
    }

    /**
     * Triggers user-defined change actions and updates the UI.
     * @param {Date | null} selected - The selected date.
     */
    TriggerUserChange(selected) {
        if (this.Disabled) {
            return;
        }
        let oldVal = this.value;
        this.Value = selected;
        /** @type {ObservableArgs} */
        var arg = { NewData: this.value, OldData: oldVal, EvType: "change" };
        this.UserInput?.invoke(arg);
        this.PopulateFields();
        this.CascadeField();
        this.simpleNoEvent = true;
        this.Input.focus();
    }

    /**
 * Sets the day selected by the user and updates the internal state.
 * @param {Date} selected - The day selected by the user.
 */
    SetSelectedDay(selected) {
        if (this.value) {
            selected = new Date(selected.getFullYear(), selected.getMonth(), selected.getDate(), this.value.getHours(), this.value.getMinutes());
        } else {
            selected = new Date(selected.getFullYear(), selected.getMonth(), selected.getDate(), this.someday.getHours(), this.someday.getMinutes());
        }
        this.TriggerUserChange(selected);
    }

    /**
     * Increases the time by a specified amount for hours or minutes.
     * @param {number} value - The amount to increase the time by.
     * @param {boolean} minute - Whether to increase minutes instead of hours.
     */
    IncreaseTime(value, minute = false) {
        let time = this.value || this.someday;
        if (!minute) {
            time.setHours(time.getHours() + value);
            this.hour.value = time.getHours().toString().padStart(2, '0');
        } else {
            time.setMinutes(time.getMinutes() + value);
            this.minute.value = time.getMinutes().toString().padStart(2, '0');
        }
        this.TriggerUserChange(time);
    }

    /**
     * Changes the hour based on user input from the hour input field.
     * @param {Event} e - The event object, containing the user input.
     */
    ChangeHour(e) {
        let newHour = parseInt(this.hour.value);
        if (isNaN(newHour) || newHour < 0 || newHour > 23) {
            return;
        }
        let time = (this.value || this.someday);
        time.setHours(newHour);
        this.TriggerUserChange(time);
    }

    /**
     * Changes the minute based on user input from the minute input field.
     * @param {Event} e - The event object, containing the user input.
     */
    ChangeMinute(e) {
        let newMinute = parseInt(this.minute.value);
        if (isNaN(newMinute) || newMinute < 0 || newMinute > 59) {
            return;
        }
        let time = (this.value || this.someday);
        time.setMinutes(newMinute);
        this.TriggerUserChange(time);
    }

    /**
     * Handles keyboard shortcuts for changing hours and minutes.
     * @param {Event} e - The event object.
     */
    ChangeHourMinuteHotKey(e) {
        if (e.keyCode === 38) { // Arrow Up
            this.IncreaseTime(1, e.target === this.minute);
        } else if (e.keyCode === 40) { // Arrow Down
            this.IncreaseTime(-1, e.target === this.minute);
        }
    }

    /**
     * Validates the current date value against defined rules.
     * @returns {Promise<boolean>} A promise that resolves to true if the validation passes.
     */
    ValidateAsync() {
        if (!this.ValidationRules || this.ValidationRules.Nothing()) {
            return Promise.resolve(true);
        }
        let isValid = true;
        let rules = ['GreaterThan', 'LessThan', 'GreaterThanOrEqual', 'LessThanOrEqual', 'Equal', 'NotEqual'];
        rules.forEach(rule => {
            isValid = isValid && this.Validate(rule, this.value);
        });
        this.IsValid = isValid;
        return Promise.resolve(isValid);
    }

    /**
     * Generic validation method to apply different types of validation rules.
     * @param {string} rule - The rule to apply.
     * @param {Date} value - The value to validate.
     * @returns {boolean} Whether the value passes the validation.
     */
    Validate(rule, value) {
        // Example: Extend to include specific rule validation
        switch (rule) {
            case 'GreaterThan':
                return value > new Date(this.Meta.MinDate);
            case 'LessThan':
                return value < new Date(this.Meta.MaxDate);
            default:
                return true;
        }
    }

    /**
     * Removes the datepicker and its elements from the DOM.
     */
    RemoveDOM() {
        if (!this.Element?.parentElement) return;
        this.Element.parentElement.innerHTML = string.Empty;
        this.Element = null;
    }

    /**
     * Sets the UI of the datepicker to either enabled or disabled based on the given value.
     * @param {boolean} value - Whether the datepicker should be disabled.
     */
    SetDisableUI(value) {
        if (this.Input) {
            this.Input.readonly = value;
        }
        return this.FieldVal?.toString();
    }
}

import { ObservableArgs, Component, keyCodeEnum, EventType } from "./models/";
import { EditableComponent } from "./editableComponent.js";
import { Str } from "./utils/ext.js";
import { html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ComponentExt } from "./utils/componentExt.js";
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import customParseFormat from 'dayjs/plugin/customParseFormat';
import localizedFormat from 'dayjs/plugin/localizedFormat';
import { searchMethodEnum } from "./models/enum.js";
import "flatpickr/dist/flatpickr.min.css";
import flatpickr from "flatpickr";
import { vietnamese } from "flatpickr/dist/l10n/vn.js"
import { LangSelect } from "./index.js";
// extend dayjs with the necessary plugins
dayjs.extend(utc);
dayjs.extend(customParseFormat);
dayjs.extend(localizedFormat);
export class Datepicker extends EditableComponent {
    /** @type {HTMLElement} */
    calendar = null;
    renderAwaiter = null;
    closeAwaiter = null;
    /** @type {dayjs.dayjs} */
    value;
    /**
     * create instance of component
     * @param {Component} ui 
     * @param {HTMLElement} ele 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.defaultValue = dayjs();
        /** @type {Component} */
        this.initFormat = this.meta.formatData?.includes("{0:") ? this.meta.formatData.replace("{0:", "").replace("}", "") : (this.meta.precision === 7 ? "dD/mM/yYYY hH:mm" : "dD/mM/yYYY");
        this.currentFormat = this.initFormat;
        if (ele != null) {
            if (ele.firstElementChild instanceof hTMLInputElement) {
                this.input = ele.firstElementChild;
            } else {
                this.input = html.take(ele).input.context;
            }
        }
        this.value = null;
        this.nullable = false;
        this.simpleNoEvent = false;
        this.show = false;
        this.someday = dayjs();
        this.hour = null;
        this.minute = null;
        this.searchMethod = searchMethodEnum.equal;
        this.searchIcon = "fas fa-equals";
        this.searchIconElement = null;
    }

    /**
     * gets the value of the datepicker.
     * @returns {dayjs.dayjs | null}
     */
    get value() {
        return this.value;
    }

    /**
     * sets the value of the datepicker and triggers related updates.
     * @param {dayjs.dayjs | null} value - the new date value.
     */
    set value(value) {
        if (this.value === value) {
            return;
        }
        if (!value) {
            this.input.value = "";
            this.entity[this.name] = null;
            this.flatpickr.setDate(null);
            return;
        }
        this.value = value;
        const selectionEnd = this.input.selectionEnd;
        var data = Utils.isFunction(this.meta.formatEntity, false, this);
        if (data) {
            this.input.value = data;
        }
        else {
            this.input.value = this.value.format(this.initFormat)
        }
        this.flatpickr.setDate(this.input.value);
        this.input.selectionStart = selectionEnd;
        this.input.selectionEnd = selectionEnd;
        if (this.meta.precision == 7) {
            this.entity[this.name] = this.value.format('yYYY-mM-dDTHH:mm:ss');
        }
        else {
            this.entity[this.name] = this.dayjs(this.value.format('yYYY-mM-dD'), 'yYYY-mM-dD').format('yYYY-mM-dDTHH:mm:ss');
        }
    }
    /**
     * sets the value of the datepicker and triggers related updates.
     * @type {hTMLInputElement} value - the new date value.
     */
    input;
    /**@type {flatpickr} */
    flatpickr
    /**
     * renders the datepicker component in the dOM.
     */
    render() {
        this.setDefaultVal();
        html.take(this.parentElement);
        if (!this.input) {
            html.div.className("datetime-picker").tabIndex(-1);
            html.input.render();
            this.element = html.context;
            this.input = this.element;
        } else {
            html.take(this.input);
            this.element = this.input;
        }
        html.event("keydown", (e) => {
            if (this.disabled || !e) return;
            if (e.keyCode === 13) {
                this.parseDate();
            }
        }).event("change", () => this.parseDate())
            .placeHolder(this.meta.plainText).attr("autocomplete", "off")
            .attr('name', this.name);
        if (!this.meta.showHotKey) {
            html.end.div.className("btn-group").button.tabIndex(-1).span.className("fal fa-calendar")
                .event("click", (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (this.input.disabled) return;
                    if (this.flatpickr.isOpen) {
                        this.flatpickr.close();
                    } else {
                        this.flatpickr.open();
                    }
                });
        }
        const scrollElement = this.element.closest(".scroll-content");
        var mode = "single";
        if (this.meta && this.meta.precision === 2) {
            mode = "range";
        }
        if (this.meta && this.meta.isMultiple) {
            mode = "multiple";
        }
        var seft = this;
        this.flatpickr = flatpickr(this.input, {
            enableTime: this.meta.precision === 7,
            allowInput: true,
            weekNumbers: true,
            locale: LangSelect.culture == "vi" ? vietnamese : null,
            dateFormat: (this.meta.precision === 7 ? "d/m/y h:i" : "d/m/y"),
            clickOpens: !this.meta.focusSearch,
            mode: mode,
            time_24hr: this.meta.precision === 7 ? true : false,
            onOpen: () => {
                if (scrollElement) {
                    scrollElement.addEventListener(EventType.scroll, this.repositionFlatpickr.bind(this), true);
                }
            },
            onChange: function (selectedDates, dateStr, instance) {
                if (seft.meta.precision == 2) {
                    if (selectedDates[0]) {
                        seft.entity[seft.meta.fieldName] = selectedDates[0];
                    }
                    else {
                        seft.entity[seft.meta.fieldName] = null;
                    }
                    if (selectedDates[1]) {
                        seft.entity[seft.meta.fieldName + "to"] = selectedDates[1];
                    }
                    else {
                        seft.entity[seft.meta.fieldName + "to"] = null;
                    }
                }
            },
            onClose: () => {
                if (scrollElement) {
                    scrollElement.removeEventListener(EventType.scroll, this.repositionFlatpickr.bind(this), true);
                }
            }
        });
        let str = null;
        if (this.entity[this.name]) {
            str = dayjs(this.entity[this.name]).format(this.initFormat);
            this.value = dayjs(this.entity[this.meta.fieldName]);
            this.flatpickr.setDate(str);
        }
        else {
            this.value = null;
            this.flatpickr.setDate(null);
        }
        this.input.value = str;
        this.originalText = str;
        this.oldValue = this.entity[this.meta.fieldName];
        if (this.meta.precision !== 7 && this.value) {
            this.entity[this.name] = this.dayjs(this.value.format('yYYY-mM-dD'), 'yYYY-mM-dD').format('yYYY-mM-dDTHH:mm:ss');
        }
        this.dOMContentLoaded?.invoke();
    }

    repositionFlatpickr() {
        if (this.flatpickr && this.flatpickr.isOpen) {
            this.flatpickr._positionCalendar();
        }
    };

    /**
     * handles key down events specifically for managing date and time inputs.
     * @param {event} e - the event object.
     */
    keyDownDateTime(e) {
        if (e.keyCodeEnum() === keyCodeEnum.enter && this.value === null) {
            if (this.disabled) {
                return;
            }
            if (this.show) {
                this.closeCalendar();
            } else {
                this.renderCalendar();
            }
        }
    }

    /**
     * determines if a given type is nullable.
     * @param {string} type - the type to check.
     * @returns {boolean} whether the type is nullable.
     */
    isNullable(type) {
        return this.entity === null || Utils.isNullable(this.name, this.entity);
    }

    /**
     * parses the date from the input and sets the component's value.
     */
    parseDate() {
        if (this.meta.precision == 2) {
            return;
        }
        if (Utils.isNullOrWhiteSpace(this.input.value)) {
            this.input.value = "";
            this.triggerUserChange(null);
            return;
        }
        const { parsed, dateTime, format } = this.tryParseDateTime(this.input.value);
        if (!parsed) {
            if (this.editForm.meta.customNextCell) {
                return;
            }
            this.input.value = "";
            this.triggerUserChange(null);
        } else {
            this.value = dateTime;
            this.triggerUserChange(dateTime);
        }
    }

    /**
 * attempts to parse a datetime string using known formats.
 * @param {string} value - the datetime string to parse.
 * @returns {{parsed: boolean, datetime: date | null}}
 */
    tryParseDateTime(value) {
        let dateTime = dayjs();
        let parsed = false;
        let format = null;
        var length = value.length;
        switch (length) {
            case 4:
                dateTime = dayjs(value, "dDMM", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dDMM";
                }
                break;
            case 5:
                dateTime = dayjs(value, "dD/mM", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dD/mM";
                }
                break;
            case 8:
                dateTime = dayjs(value, "dDMMYYYY", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dDMMYYYY";
                }
                break;
            case 10:
                dateTime = dayjs(value, "dD/mM/yYYY", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dD/mM/yYYY";
                }
                break;
            case 16:
                dateTime = dayjs(value, "dD/mM/yYYY hH:mm", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dD/mM/yYYY hH:mm";
                }
                break;
            default:
                dateTime = dayjs(value, "dDMM", dayjs()); // true for strict parsing
                if (dateTime.isValid()) {
                    parsed = true;
                    format = "dDMM";
                    break;
                }
                break;
        }
        return { parsed, dateTime, format };
    }

    /**
     * triggers user-defined change actions and updates the uI.
     * @param {dayjs.dayjs | null} selected - the selected date.
     */
    triggerUserChange(selected, input = false) {
        if (this.disabled) {
            return;
        }
        let oldVal = this.value;
        if (this.hour) {
            this.value = selected.hour(this.hour);
        }
        if (this.minute) {
            this.value = selected.minute(this.minute);
        }
        if (!this.hour && !this.minute) {
            this.value = selected;
        }
        this.dirty = true;
        /** @type {ObservableArgs} */
        // @ts-ignore
        if (!input) {
            var arg = { newData: this.value, oldData: oldVal, evType: "change" };
            this.dispatchEvent(this.meta.events, EventType.change, this, this.entity).then(() => {
                this.userInput?.invoke(arg);
                this.populateFields();
                this.cascadeField();
                this.simpleNoEvent = true;
                if (this.parent.isListViewItem) {
                    if (this.meta.precision != 7) {
                        this.input.focus();
                    }
                }
                else {
                    var rangeCom = this.editForm.childCom.filter(x => !x.isButton && !x.isListView);
                    var index = rangeCom.indexOf(this);
                    if (rangeCom[index + 1]) {
                        rangeCom[index + 1].focus();
                    }
                }
            });
        }
    }
    /**
     * triggers user-defined change actions and updates the uI.
     * @param {dayjs.dayjs | null} selected - the selected date.
     */
    triggerRangeChange(selecteds, input = false) {
        if (this.disabled) {
            return;
        }
        this.dirty = true;
        /** @type {ObservableArgs} */
        // @ts-ignore
        if (!input) {
            var arg = { newData: this.value, oldData: oldVal, evType: "change" };
            this.dispatchEvent(this.meta.events, EventType.change, this, this.entity).then(() => {
                this.userInput?.invoke(arg);
                this.populateFields();
                this.cascadeField();
                this.simpleNoEvent = true;
                if (this.parent.isListViewItem) {
                    this.input.focus();
                }
                else {
                    var rangeCom = this.editForm.childCom.filter(x => !x.isButton && !x.isListView);
                    var index = rangeCom.indexOf(this);
                    if (rangeCom[index + 1]) {
                        rangeCom[index + 1].focus();
                    }
                }
            });
        }

    }

    /**
     * sets the day selected by the user and updates the internal state.
     * @param {dayjs.dayjs} selected - the day selected by the user.
     */
    setSelectedDay(evt, selected) {
        this.closeCalendar();
        this.triggerUserChange(selected);
    }

    /**
     * increases the time by a specified amount for hours or minutes.
     * @param {number} value - the amount to increase the time by.
     * @param {boolean} minute - whether to increase minutes instead of hours.
     */
    increaseTime(value, minute = false) {
        let time = this.value || this.someday;
        if (!minute) {
            time.setHours(time.get('hour') + value);
            this.hour = time.get('hour').toString().padStart(2, '0');
        } else {
            time.setMinutes(time.get('minute') + value);
            this.minute = time.get('minute').toString().padStart(2, '0');
        }
        this.triggerUserChange(time);
    }

    /**
     * changes the hour based on user input from the hour input field.
     * @param {event} e - the event object, containing the user input.
     */
    changeHour(e) {
        let newHour = parseInt(e.target.value || "0");
        if (newHour < 0 || newHour > 23) {
            return;
        }
        let time = (this.value || this.someday);
        this.hour = newHour;
        var newDate = time.hour(newHour);
        this.triggerUserChange(newDate, true);
    }

    /**
     * changes the minute based on user input from the minute input field.
     * @param {event} e - the event object, containing the user input.
     */
    changeMinute(e) {
        let newMinute = parseInt(e.target.value || "0");
        if (newMinute < 0 || newMinute > 59) {
            return;
        }
        let time = (this.value || this.someday);
        this.minute = newMinute;
        var newDate = time.minute(newMinute);
        this.triggerUserChange(newDate, true);
    }

    /**
     * handles keyboard shortcuts for changing hours and minutes.
     * @param {event} e - the event object.
     */
    changeHourMinuteHotKey(e) {
        if (e.keyCode() === keyCodeEnum.upArrow) { // arrow up
            this.increaseTime(1, e.target === this.minute);
        } else if (e.keyCode() === keyCodeEnum.downArrow) { // arrow down
            this.increaseTime(-1, e.target === this.minute);
        }
    }

    async validateAsync() {
        if (this.validationRules.length == 0) {
            return true;
        }
        this.validationResult = [];
        this.validateRequired(this.value);
        return this.isValid;
    }

    /**
     * generic validation method to apply different types of validation rules.
     * @param {string} rule - the rule to apply.
     * @param {date} value - the value to validate.
     * @returns {boolean} whether the value passes the validation.
     */
    validate(rule, value) {
        // example: extend to include specific rule validation
        switch (rule) {
            case 'gt':
                return value > dayjs(this.meta.minDate);
            case 'lt':
                return value < dayjs(this.meta.maxDate);
            default:
                return true;
        }
    }

    /**
     * removes the datepicker and its elements from the dOM.
     */
    removeDOM() {
        if (!this.element?.parentElement) return;
        this.element.parentElement.innerHTML = Str.empty;
        this.element = null;
    }

    /**
     * sets the uI of the datepicker to either enabled or disabled based on the given value.
     * @param {boolean} value - whether the datepicker should be disabled.
     */
    setDisableUI(value) {
        if (this.input) {
            this.input.disabled = value;
        }
    }

    updateView(force = false, dirty = null, ...componentNames) {
        var newValue = this.entity[this.meta.fieldName];
        if (newValue != this.oldValue) {
            if (newValue) {
                try {
                    this.value = dayjs(newValue, 'yYYY-mM-dDTHH:mm:ss');
                } catch {
                    this.value = null;
                }
            }
            else {
                this.value = null;
            }
            if (!this.dirty) {
                this.originalText = this.input.value;
                this.dOMContentLoaded?.invoke();
                this.oldValue = this.input.value;
            }
        }
    }

    setDefaultVal() {
        if (Utils.isNullOrWhiteSpace(this.meta.defaultVal)) {
            return;
        }
        var data = Utils.isFunction(this.meta.defaultVal, true, this);
        if (!data) {
            data = this.meta.defaultVal;
        }
        if (data && this.entity[this.name] == null && this.entity[this.idField].toString().startsWith("-")) {
            if (dayjs.isDayjs(this.entity[this.name])) {
                if (this.meta.precision == 7) {
                    this.entity[this.name] = this.value.format('yYYY-mM-dDTHH:mm:ss');
                }
                else {
                    this.entity[this.name] = this.dayjs(this.value.format('yYYY-mM-dD'), 'yYYY-mM-dD').format('yYYY-mM-dDTHH:mm:ss');
                }
            }
            else {
                this.entity[this.name] = data;
            }
        }
    }
}

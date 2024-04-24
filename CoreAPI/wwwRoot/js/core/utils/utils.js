import { Decimal } from '../structs/decimal.js';
import { Component } from "../models/component.js";

export class Utils {
    static SystemId = "1";
    static TenantField = "t";
    static Pixel = "px";
    static FeatureField = "f";
    static QuestionMark = "?";
    static Amp = "&";
    static BreakLine = "<br />";
    static ApplicationJson = "application/json";
    static Authorization = "Authorization";
    static SelfVendorId = "65";
    static IdField = "Id";
    static NewLine = "\r\n";
    static Indent = "\t";
    static Dot = ".";
    static Slash = "/";
    static Hash = "#";
    static Comma = ";";
    static Semicolon = ";";
    static Space = " ";
    static ComponentId = "20";
    static ComponentGroupId = "30";
    static HistoryId = "4199";
    static InsertedBy = "InsertedBy";
    static OwnerUserIds = "OwnerUserIds";
    static OwnerRoleIds = "OwnerRoleIds";
    static ComQuery = "/user/comQuery";
    static PatchSvc = "/v2/user";
    static PatchesSvc = "user/SavePatches";
    static UserSvc = "/user/svc";
    static DeleteSvc = "/user/del";
    static DeactivateSvc = "/user/Deactivate";
    static ExportExcel = "/user/excel";
    static FileSvc = "/user/file";
    static Return = "return ";
    static SpecialChar = {
        '+': "%2B",
        '/': "%2F",
        '?': "%3F",
        '#': "%23",
        '&': "%26"
    };
    static ReverseSpecialChar = {
        "%2B": '+',
        "%2F": '/',
        "%3F": '?',
        "%23": '#',
        "%26": '&'
    }
    static EncodeSpecialChar(str) {
        if (!str) return null;
        return str.split('').map(ch => Utils.SpecialChar[ch] || ch).join('');
    }
    static DecodeSpecialChar(str) {
        if (!str) return null;
        return str.replace(/%2B/g, '+')
            .replace(/%2F/g, '/')
            .replace(/%3F/g, '?')
            .replace(/%23/g, '#')
            .replace(/%26/g, '&');
    }
    static GenerateRandomToken(maxLength = 32) {
        let builder = '';
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        const charsLength = chars.length;
        for (let i = 0; i < maxLength; i++) {
            builder += chars.charAt(Math.floor(Math.random() * charsLength));
        }
        return builder;
    }
    static ToJson(value) {
        return JSON.stringify(value);
    }
    static Clone(value) {
        return JSON.parse(JSON.stringify(value));
    }
    static TryParseInt(value) {
        const parsed = parseInt(value, 10);
        return isNaN(parsed) ? null : parsed;
    }
    /**
     * 
     * @param {string} value - String value to parsed
     * @returns {[boolean, number?]} - Returns an array with a boolean indicating if the parsing was successful and the parsed decimal value
     */
    static TryParseDecimal(value) {
        try {
            let res = new Decimal(value);
            return [true, res];
        } catch {
            return [false, null];
        }
    }
    static Parse(value) {
        try {
            return JSON.parse(value);
        } catch {
            return null;
        }
    }
    static TryParse(value) {
        try {
            return JSON.parse(value);
        } catch {
            return null;
        }
    }
    static ChangeType(value, type) {
        switch (type) {
            case 'int':
            case 'number':
                return Utils.TryParseInt(value);
            case 'decimal':
                return Utils.TryParseDecimal(value);
            case 'boolean':
                return value === 'true';
            default:
                return value;
        }
    }
    static EncodeProperties(value) {
        if (!value || typeof value !== 'object') return value;

        Object.keys(value).forEach(prop => {
            if (typeof value[prop] === 'string') {
                value[prop] = Utils.EncodeSpecialChar(value[prop]);
            }
        });

        return value;
    }

    static DecodeProperties(value) {
        if (!value || typeof value !== 'object') return value;

        Object.keys(value).forEach(prop => {
            if (typeof value[prop] === 'string') {
                value[prop] = Utils.DecodeSpecialChar(value[prop]);
            }
        });

        return value;
    }

    static GetUrlParam(key = Utils.FeatureField, origin = null) {
        const search = new URLSearchParams(origin || window.location.search);
        return search.get(key);
    }

    static GetEntityById(id) {
        return Client.Entities[id];
    }

    static GetEntity(name) {
        return Object.values(Client.Entities).find(entity => entity.Name === name);
    }


    /**
    * Checks if a complex property is nullable.
    * @param {string} fieldName - The property name to check.
    * @param {Object} obj - The object containing the property.
    * @returns {boolean} True if the property is nullable, false otherwise.
    */
    static IsNullable(fieldName, obj) {
        const type = Utils.GetPropValue(fieldName, obj);
        return !type || type === typeof (null);
    }


    static GetComplexPropType(fieldName, obj) {
        if (!fieldName || !obj || typeof obj !== 'object') return null;

        const props = fieldName.split('.');
        let current = obj;

        for (const prop of props) {
            if (!current.hasOwnProperty(prop)) {
                return null;
            }
            current = current[prop];
        }

        return typeof current;
    }

    static GetPropValue(obj, propName) {
        if (!obj || !propName || typeof obj !== 'object') return null;

        const props = propName.split('.');
        let current = obj;

        for (const prop of props) {
            if (!current.hasOwnProperty(prop)) {
                return null;
            }
            current = current[prop];
        }

        return current;
    }

    static GetCellText(header, cellData, row, emptyRow = false) {
        return Utils.GetCellTextInternal(header, cellData, row, emptyRow).DecodeSpecialChar();
    }

    /**
     * 
     * @param {Component} header 
     * @param {any} cellData 
     * @param {any} row 
     * @param {boolean} emptyRow 
     * @returns 
     */
    static GetCellTextInternal(header, cellData, row, emptyRow = false) {
        const dt = new Date();
        const isDate = cellData && header.ComponentType === 'Datepicker' && !isNaN(Date.parse(cellData));
        const isRef = header.FieldText && header.FieldText.trim().length > 0 || header.LocalData || header.ReferenceId;

        if (emptyRow) {
            return '';
        }
        if (header.FieldName === Utils.IdField && cellData && typeof cellData === 'number' && cellData <= 0) {
            return '';
        }
        if (!isRef && header.FormatEntity) {
            var fn = Utils.IsFunction(header.FormatEntity);
            if (fn) {
                return fn.call(row, row);
            }
            return Utils.GetFormattedRow(header.FormatEntity, row);
        } else if (cellData === null || cellData === undefined) {
            return header.PlainText || '';
        } else if (cellData instanceof Date || isDate) {
            return (new Date(cellData)).toLocaleDateString();
        } else if (isRef) {
            return row[header.FieldText];
        } else if (header.FormatData) {
            return header.FormatData.replace('{0}', cellData);
        } else {
            return cellData.toString();
        }
    }

    static GetHtmlCode(format, source, nullHandler = Utils.NullFormatHandler, notFoundHandler = Utils.NotFoundHandler) {
        if (!format) return null;

        const objList = [];
        let index = 0;
        let isInGroup = false;
        let field = '';
        let formatted = '';

        for (let i = 0; i < format.length; i++) {
            const ch = format[i];
            switch (ch) {
                case '$':
                    if (format[i + 1] === '{') {
                        isInGroup = true;
                        formatted += '{' + index.toString();
                    } else {
                        if (isInGroup) {
                            field += ch;
                        } else {
                            formatted += ch;
                        }
                    }
                    break;
                case '}':
                    if (isInGroup) {
                        isInGroup = false;
                        formatted += ch;
                        index++;
                        Utils.GetValues(source[0], nullHandler, notFoundHandler, field, objList);
                        field = '';
                    } else {
                        formatted += ch;
                    }
                    break;
                default:
                    if (isInGroup && ch === '{') {
                        break;
                    }
                    if (isInGroup) {
                        field += ch;
                    } else {
                        formatted += ch;
                    }
                    break;
            }
        }
        return formatted;
    }

    static GetFormattedRow(exp, row) {
        const isFunc = Utils.IsFunction(exp);
        if (!isFunc) {
            return Utils.FormatEntity(exp, null, row, Utils.EmptyFormat, Utils.EmptyFormat);
        }
        return exp(row, row)?.toString();
    }

    static ForEachProp(obj, action) {
        if (!obj || typeof obj !== 'object' || typeof action !== 'function') return;
        const props = Object.keys(obj);
        props.forEach(prop => action(prop, obj[prop]));
    }

    static LastDayOfMonth(time = null) {
        const current = time ? new Date(time) : new Date();
        const year = current.getFullYear();
        const month = current.getMonth() + 1;
        return new Date(year, month, 0);
    }

    /**
     *
     * @param {String | Function} exp - The expression represent function, or a function
     * @param {boolean} shouldAddReturn - if true then append 'return ' before evaluating
     * @returns {Function} The function itself or evaluated function
     */
    static IsFunction(exp, shouldAddReturn) {
        if (exp == null) {
            return null;
        }
        if (exp instanceof Function) {
            return exp;
        }
        try {
            var fn = new Function(shouldAddReturn ? "return " + exp : exp);
            const fnVal = fn.call(null);
            if (fnVal instanceof Function) {
                return fnVal;
            } else {
                return null;
            }
        } catch ($e1) {
            return null;
        }
    }
}

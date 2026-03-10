/**
* Represents a wrapper around an XMLHttpRequest (XHR) object, providing additional functionality and configuration options.
*
* The `XHRWrapper` class encapsulates the properties and methods necessary to make HTTP requests, handle responses, and provide customization options.
*
* @property {boolean} allowNested - Determines whether nested requests are allowed.
* @property {boolean} noQueue - Indicates whether the request should bypass the queue.
* @property {boolean} retry - Specifies whether the request should be retried on failure.
* @property {boolean} showError - Determines whether errors should be displayed.
* @property {boolean} allowAnonymous - Indicates whether anonymous requests are allowed.
* @property {boolean} addTenant - Specifies whether the tenant information should be added to the request.
* @property {string} method - The HTTP method to be used for the request (default is 'GET').
* @property {string} url - The URL for the request.
* @property {string} nameSpace - The namespace for the request.
* @property {string} prefix - The prefix for the request.
* @property {string} entityName - The name of the entity for the request.
* @property {string} finalUrl - The final URL for the request, including any necessary modifications.
* @property {string} responseMimeType - The expected MIME type of the response.
* @property {any} value - The value to be sent with the request.
* @property {boolean} isRawString - Indicates whether the `value` property should be treated as a raw string.
* @property {Map<string, string>} headers - The headers to be included in the request.
* @property {FormData} formData - The form data to be sent with the request.
* @property {File} file - The file to be sent with the request.
* @property {function} progressHandler - A callback function to handle progress events.
* @property {function} customParser - A custom parser function for the response.
* @property {function} errorHandler - A custom error handling function.
*
* @constructor
*/
export default class XHRWrapper {
    allowNested = false;
    noQueue = false;
    /** @type {boolean?} */
    retry = false;
    /** @type {boolean?} */
    showError = true;
    allowAnonymous = false;
    addTenant = false;
    method = 'GET'; // Default HTTP method
    /** @type {string} */
    url = '';
    nameSpace = '';
    prefix = '';
    entityName = '';
    finalUrl = '';
    responseMimeType = '';
    value = null;
    isRawString = false;

    constructor() {
        /** @type {any} */
        this.headers = {};
        this.formData = null;
        this.file = null;
        this.progressHandler = null;
        this.customParser = null;
        this.errorHandler = null;
    }

    get jsonData() {
        if (this.value === null) {
            return null;
        }
        if (this.isRawString && typeof this.value === 'string') {
            return this.value;
        }
        return JSON.stringify(this.value);
    }

    static unboxValue(val) {
        if (val === null) return null;
        let res = {};
        for (let key in val) {
            if (key === null || key[0] === '$') continue;
            let item = val[key];
            if (item !== null && item !== undefined) {
                const type = typeof item;
                let isSimple = (type === 'number' || type === 'boolean' || type === 'string') ||
                    (item instanceof Date) ||
                    (typeof item === 'object' && Object.prototype.toString.call(item) === '[object Date]') ||
                    (item.constructor.name === 'Date') ||
                    (type === 'object' && (item.toString() === '[object Date]')) ||
                    (!isNaN(parseFloat(item)) && isFinite(item));

                if (isSimple) {
                    res[key] = item;
                }
            }
        }
        return res;
    }
}

export class StringBuilder {
    constructor(initialString = '') {
        this._buffer = [initialString];
    }

    append(str) {
        this._buffer.push(str);
        return this; // for method chaining
    }

    toString() {
        return this._buffer.join('');
    }
}
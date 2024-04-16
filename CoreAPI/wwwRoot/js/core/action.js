export class Action {
    /** @type {Function[]} handler - An array of event handler functions. */
    handler;
    constructor() {
        this.handler = [];
    }
    add(handler) {
        this.handler.push(handler);
    }
    Invoke() {
        this.handler.forEach(h => h());
    }
}
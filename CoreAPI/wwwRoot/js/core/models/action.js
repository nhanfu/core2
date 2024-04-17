/**
 * @typedef {import('./observable.js').default}  ObservableArgs
 */
export class Action {
    /** @type {Function[]} handler - An array of event handler functions. */
    handler;
    constructor() {
        this.handler = [];
    }
    add(handler) {
        this.handler.push(handler);
    }
    /**
     * 
     * @param {ObservableArgs} observable 
     */
    Invoke(observable) {
        this.handler.forEach(h => h(observable));
    }
}
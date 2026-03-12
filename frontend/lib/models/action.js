/**
 * @typedef {import('./observable.js').default}  ObservableArgs
 */
export class Action {
    hasTrigger = false;
    /** @type {Array<(item: ObservableArgs) => void>} handler - An array of event handler functions. */
    handler = [];
    constructor() {
    }
    /**
     * @param {{ (): void; (item: import("./observable.js").default): void; }} handler
     */
    add(handler) {
        this.handler.push(handler);
    }
    
    remove(handler) {
        this.handler = this.handler.filter(h => h !== handler);
    }
    /**
     * @param {any | import("./observable.js").default} [observable]
     */
    invoke(observable) {
        this.hasTrigger = true;
        this.handler?.forEach(h => h(observable));
    }
}
import { html, events } from '../ngin/html.js';

export class Factory {
    #ele = null;
    constructor(meta, env) {
        this.meta = meta;
        this.env = env;
    }

    render() {
        const meta = this.meta;
        html.take(this.env.ele).input.value(this.meta.val)
            .event(events.change, (e) => meta.events.change({ com: this, event: e }));
        this.#ele = html.ctx;
    }

    get ele() { return this.#ele; }
    
    static create(meta, env) { return new Factory(meta, env); }
}
import { html, events } from '../ngin/html.js';

export class Factory {
    #ele = null;
    constructor(meta, env) {
        this.meta = meta;
        this.env = env;
    }

    render() {
        const meta = this.meta;
        html.take(this.env.ele).button.text(this.meta.label)
            .event(events.click, (e) => meta.events.click({ com: this, event: e }));
        this.#ele = html.ctx;
    }

    get ele() { return this.#ele; }

    static create(meta, env) { return new Factory(meta, env); }
}
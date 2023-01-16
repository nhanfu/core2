export class Form {
    constructor(meta, env) {
        this.meta = meta;
        this.env = env || { ele: document.body };
    }

    async render() {
        const comTask = this.meta.map(async x => x.task = await import(x.com));
        await Promise.all(comTask);
        this.meta.map(meta => {
            const instance = meta.task.Factory.create(meta, this.env);
            delete meta.task;
            instance.render();
        });
    }
}
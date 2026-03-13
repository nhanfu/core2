import { Section } from './section.js';
export class ListViewSection extends Section {
    /** @typedef {import('./listView.js').listView} ListView */
    /** @type {ListView} */
    ListView;
    render() {
        // @ts-ignore
        this.listView = this.parent;
        super.render();
    }
}
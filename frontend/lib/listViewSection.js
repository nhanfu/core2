import { Section } from './section.js';
export class ListViewSection extends Section {
    /** @typedef {import('./listView.js').listView} ListView */
    /** @type {ListView} */
    ListView;
    Render() {
        // @ts-ignore
        this.listView = this.Parent;
        super.render();
    }
}
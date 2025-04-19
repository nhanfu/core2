import { EditForm } from "../../lib/editForm";
import { Metadata } from "./productMeta";
import { Section } from "../../lib";
export default class ProductListPage extends EditForm {
    constructor(entity = null) {
        super(entity);
        this.Element = document.getElementById('app');
        this.Meta = Metadata;
        this.SectionMd = { Section: Section };
    }
}
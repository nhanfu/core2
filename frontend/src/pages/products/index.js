import meta from "./meta";
import { Client, Html, EditForm, ListViewItem, ListView } from "../../../lib";

export default class ProductListPage extends EditForm {
    constructor(entity = null) {
        super(entity);
        this.Element = document.getElementById('app');
        this.Meta = meta(this);
        this.Entity.Search = 'Some text';
    }

    Search(e) {
        /** @type {ListView} */
        const listView = this.listView;
        listView.Search(term);
    }

    /**
     * Deltete a product item
     * @param {Object} e
     * @param {ListViewItem} e.section 
     */
    RenderItem({ section }) {
        const data = section.Entity;
        Html.Instance.Div.ClassName('product-item')
            .H2.Text(data.Name).End
            .P.Text(`Price: ${data.Price}`).End
            .P.Text(`Description: ${data.Description}`).End
            .Button.Text('⌫').Roles('BOD')
            .Event('click', async () => {
                this.DeleteProduct(section);
            }, section).End.End;
    }

    /**
     * Deltete a product item
     * @param {ListViewItem} section 
     */
    async DeleteProduct(section) {
        const success = await Client.Instance
            .SubmitAsync({
                AllowAnonymous: true,
                ComId: section.ListView.Meta.Id, Action: 'delete', Id: data.Id
            })
        if (success) {
            li.Dispose();
        }
    }
}
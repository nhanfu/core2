import meta from "./meta";
import { Client, Html, EditForm, ListViewItem, ListView, Utils } from "../../../lib";
import { HttpMethod, XHRWrapper } from "../../../lib/models";

export default class ProductListPage extends EditForm {
    constructor(entity = null) {
        super(entity);
        this.Element = document.getElementById('app');
        this.Meta = meta(this);
        this.Entity.Search = 'Some text';
    }

    Search(e) {
        /** @type {ListView} */
        const listView = this.FindComponentByName('product-list');
        listView.Search(this.Entity.Search);
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
                await this.DeleteProduct(section);
            }, section).End.End;
    }

    /**
     * Deltete a product item
     * @param {ListViewItem} section 
     */
    async DeleteProduct(section) {
        /** @type {XHRWrapper} */
        const x = {
            AllowAnonymous: true,
            Url: Utils.ComQuery, Method: HttpMethod.POST,
            JsonData: JSON.stringify({
                Action: 'delete', ComId: section.ListView.Meta.Id, Id: section.Entity.Id
            })
        }
        const success = await Client.Instance.SubmitAsync(x);
        if (success) {
            section.Dispose();
        }
    }
}
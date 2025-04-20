import { Metadata } from "./meta";
import { Client, Html, EditForm, ListViewItem } from "../../../lib";

export default class ProductListPage extends EditForm {
    constructor(entity = null) {
        super(entity);
        this.Element = document.getElementById('app');
        this.Meta = Metadata;
    }

    /**
     * Render product item
     * @param {ListViewItem} li 
     */
    RenderItem(li) {
        const data = li.Entity;
        Html.Instance.Div.ClassName('product-item')
            .H2.Text(data.Name).End
            .P.Text(`Price: ${data.Price}`).End
            .P.Text(`Description: ${data.Description}`).End
            .Button.Text('⌫').Roles('admin')
            .Event('click', async () => {
                this.DeleteProduct(li);
            }).End.End;
    }

    /**
     * Deltete a product item
     * @param {ListViewItem} li 
     */
    async DeleteProduct(li) {
        const success = await Client.Instance
            .SubmitAsync({
                AllowAnonymous: true,
                ComId: listId, Action: 'delete', Id: data.Id
            })
        if (success) {
            li.Dispose();
        }
    }
}
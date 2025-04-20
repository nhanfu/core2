import { Feature, Html, Client, EditForm } from "../../../lib";
import { Actions } from "./actions";

const listId = 'product-list';
/** @type {Feature} */
export const Metadata = {
    IsLocal: true,
    IsPublic: true,
    Template: `
        <div class="product-list-page">
            <h1>Product List</h1>
            <div class="product-list">
                <!-- Product list will be rendered here -->
            </div>
        </div>
    `,
    ComponentGroup: [
        {
            Id: 'wrapper',
            ComponentType: 'Section',
            Components: [
                {
                    Id: listId,
                    Type: 'ListView',
                    EntityName: 'Product',
                    Actions: Actions,
                    Active: true,
                    RenderItem: (...arg) => {
                        const [listItem] = arg;
                        /** @type {EditForm} */
                        const form = listItem.EditForm;
                        listItem.EditForm.RenderItem(arg.ListViewItem);
                    }
                }
            ]
        }
    ],
};
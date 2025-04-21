import { Feature, Html, Client, EditForm } from "../../../lib";
import { Actions } from "./actions";

/** @typedef {import('./index.js').default} ProductListPage */

const listId = 'product-list';

/**
 * @param {ProductListPage} form 
 * @returns {Feature} feature
 */
function meta(form) {
    return {
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
                        FieldName: 'Search',
                        ComponentType: 'Input',
                        Events: { 'change': form.Search },
                    },
                    {
                        Id: listId,
                        ComponentType: 'ListView',
                        ApiEndpoint: document.location.origin,
                        RefName: 'Product',
                        PageSize: 10,
                        Page: 0,
                        Actions: Actions({ form }), // client and server interaction,
                        LocalData: [
                            { Name: 'Lui Vuitton', Price: 12000, Description: 'abc' },
                            { Name: 'Herme', Price: 24000, Description: 'def' },

                        ], // For the purpose of testing
                        RenderItem: form.RenderItem
                    }
                ]
            }
        ],
    }
};
export default meta;
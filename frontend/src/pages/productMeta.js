import { Feature } from "../../lib";

/** @type {Feature} */
export const Metadata = {
    IsLocal: true,
    Template: `
        <div class="product-list-page">
            <h1>Product List</h1>
            <div class="product-list">
                <!-- Product list will be rendered here -->
            </div>
        </div>
    `,
    Components: [
        {
            Type: 'ListView',
            EntityName: 'Product',
            Query: `select * from [Product]`,
            RenderContent: (li) => {
                const data = li.Entity;
                Html.Instance.AddHtml(`
                    <div class="product-item">
                        <h2>${data.Name}</h2>
                        <p>Price: ${data.Price}</p>
                        <p>Description: ${data.Description}</p>
                    </div>
                `);
            }
        }
    ],
};
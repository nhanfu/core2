export const Actions = [
    {
        Name: 'getList',
        Method: 'POST',
        Roles: ['admin', 'annonymous'],
        Cache: { timeout: 60 * 1000, key: 'product-list' },
        Server: (p) => {
            return `select * from [Product] offset ${p.Page * p.PageSize} rows fetch next ${p.PageSize} rows only`;
        },
        Client: (listView) => {
            return {
                Page: listView.Page,
                PageSize: listView.PageSize
            };
        }
    },
    {
        Name: 'delete',
        Method: 'DELETE',
        Roles: ['admin'],
        Server: (p) => {
            return `delete from [Product] where Id = ${p.Id}`;
        },
        Client: (listViewItem) => {
            return {
                Id: listViewItem.Entity.Id
            };
        }
    }
];
import { EditForm, ListViewItem } from "../../../lib";
import { SqlViewModel } from "../../../lib/models";

/**
 * 
 * @param {Object} arg 
 * @param {EditForm} arg.form
 * @returns 
 */
function Actions(arg) {
    return [
        {
            Name: 'search',
            Method: 'POST',
            Roles: ['admin', 'annonymous'],
            Cache: { timeout: 60 * 1000, key: 'product-list' },
            Server: (p) => {
                return {
                    query: `select * from [Product] where Name like @term offset @offset rows fetch next @pageSize rows only`,
                    params: {
                        term: `%${p.term}%`,
                        offset: p.Page * p.PageSize,
                        pageSize: p.PageSize
                    }
                };
            },
            /**
             * 
             * @param { Object } arguments
             * @param { SqlViewModel } arguments.sql
             * @param { EditForm } arguments.form
             * @returns {any} object
             */
            Client: ({ form, sql }) => {
                sql.Term = form.Entity.Search;
                return sql;
            }
        },
        {
            Name: 'delete',
            Method: 'DELETE',
            Roles: ['admin'],
            Server: (p) => {
                return {
                    query: `delete from [Product] where Id = '@Id'`,
                    params: {
                        Id: p.Id
                    }
                };
            },
            /**
             * Client side arguments sendind to server
             * @param { Object } arguments
             * @param { ListViewItem } arguments.listView
             * @param { EditForm } arguments.form
             * @param { SqlViewModel } arguments.sql
             * @returns {any} object
             */
            Client: ({ listItem, form, sql }) => {
                sql.Action = 'delete';
                sql.ComId = listItem.ListView.Meta.Id;
                sql.Id = listItem.Entity.Id;
                return sql;
            }
        }
    ];
}
export { Actions };
import { Client } from "../clients/client.js";
/**
 * Represents a view model for SQL operations.
 */
export class SqlViewModel {
  /**
   * @type {string}
   * @description Service ID.
   */
  svcId = "";

  /**
   * @type {string}
   * @description Company ID.
   */
  comId = "";

  /**
   * @type {string}
   * @description Action to perform.
   */
  action = "";

  /**
   * @type {string}
   * @description Parameters for the action.
   */
  params = "";

  /**
   * @type {string[]}
   * @description Array of IDs.
   */
  ids = [];

  /**
   * @type {string}
   * @description Anonymous tenant.
   */
  annonymousTenant = Client.Tenant;

  /**
   * @type {string}
   * @description Anonymous environment.
   */
  annonymousEnv = Client.Env;

  /**
   * @type {string}
   * @description Paging information.
   */
  paging = "";

  /**
   * @type {string}
   * @description SELECT clause.
   */
  select = "";

  /**
   * @type {string}
   * @description WHERE clause.
   */
  where = "";

  /**
   * @type {string}
   * @description ORDER BY clause.
   */
  orderBy = "";

  /**
   * @type {string}
   * @description GROUP BY clause.
   */
  groupBy = "";

  /**
   * @type {string}
   * @description HAVING clause.
   */
  having = "";

  /**
   * @type {boolean}
   * @description Indicates whether to include COUNT operation.
   */
  count = false;

  /**
   * @type {string[]}
   * @description Array of field names.
   */
  fieldName = [];

  /**
   * @type {boolean}
   * @description Indicates whether to skip XQuery.
   */
  skipXQuery = false;

  /**
   * @type {number}
   * @description Meta connection string.
   */
  skip = 0;

  /**
   * @type {number}
   * @description Meta connection string.
   */
  top = 0;

  /**
   * @type {string}
   * @description Meta connection string.
   */
  metaConn = Client.MetaConn;

  /**
   * @type {string}
   * @description Data connection string.
   */
  dataConn = Client.DataConn;

  /**
   * @type {string}
   * @description Name of the table.
   */
  table = "";

  /**
   * @type {boolean}
   * @description Indicates whether to wrap the query.
   */
  wrapQuery = true;
}

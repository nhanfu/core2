import pg from "npm:pg@8.11.3";

const { Pool } = pg;

/**
 * PostgreSQL client configuration
 * Initialize with connection string from environment variable
 */

const databaseUrl = Deno.env.get("DATABASE_URL");

if (!databaseUrl) {
  throw new Error("Missing environment variable: DATABASE_URL");
}

// Create connection pool
const pool = new Pool({
  connectionString: databaseUrl,
});

/**
 * Execute a raw SQL query and return results (SELECT queries)
 * @param sql - The SQL query to execute
 * @param params - Optional query parameters
 * @returns Array of result rows
 */
export async function query(sql: string, params?: any[]): Promise<any[]> {
  const client = await pool.connect();
  try {
    const result = params
      ? await client.query(sql, params)
      : await client.query(sql);
    return result.rows;
  } finally {
    client.release();
  }
}

/**
 * Execute a raw SQL statement (INSERT, UPDATE, DELETE)
 * @param sql - The SQL statement to execute
 * @param params - Optional query parameters
 * @returns Object with data and count of affected rows
 */
export async function execute(sql: string, params?: any[]): Promise<{ data: any; count: number }> {
  const client = await pool.connect();
  try {
    const result = params
      ? await client.query(sql, params)
      : await client.query(sql);
    return {
      data: result.rows,
      count: result.rowCount || 0,
    };
  } finally {
    client.release();
  }
}

/**
 * Insert a record into a table
 * @param table - The table name
 * @param data - The data to insert
 * @returns The inserted record(s)
 */
export async function insert(table: string, data: any): Promise<any> {
  const columns = Object.keys(data);
  const values = Object.values(data);
  const placeholders = columns.map((_, i) => `$${i + 1}`).join(", ");
  const sql = `INSERT INTO ${table} (${columns.join(", ")}) VALUES (${placeholders}) RETURNING *`;

  const result = await query(sql, values);
  return result;
}

/**
 * Update records in a table
 * @param table - The table name
 * @param data - The data to update
 * @param filters - The filter conditions (column-value pairs)
 * @returns The updated record(s)
 */
export async function update(table: string, data: any, filters: object): Promise<any> {
  const entries = Object.entries(data);
  const setClause = entries.map((key, i) => `${key[0]} = $${i + 1}`).join(", ");
  const filterEntries = Object.entries(filters);
  const filterStartIndex = entries.length + 1;
  const filterClause = filterEntries.map((key, i) => `${key[0]} = $${filterStartIndex + i}`).join(" AND ");

  const sql = `UPDATE ${table} SET ${setClause} WHERE ${filterClause} RETURNING *`;
  const params = [...Object.values(data), ...Object.values(filters)];

  const result = await query(sql, params);
  return result;
}

/**
 * Delete records from a table
 * @param table - The table name
 * @param filters - The filter conditions (column-value pairs)
 * @returns The deleted record(s)
 */
export async function remove(table: string, filters: object): Promise<any> {
  const filterEntries = Object.entries(filters);
  const filterClause = filterEntries.map((key, i) => `${key[0]} = $${i + 1}`).join(" AND ");

  const sql = `DELETE FROM ${table} WHERE ${filterClause} RETURNING *`;
  const result = await query(sql, Object.values(filters));
  return result;
}


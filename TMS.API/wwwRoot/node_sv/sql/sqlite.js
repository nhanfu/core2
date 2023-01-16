const sqlite3 = require('sqlite3').verbose();
const log4js = require('log4js');

log4js.configure({
    appenders: { out: { type: "file", filename: "./sql.log" } },
    categories: { default: { appenders: ["out"], level: "error" } },
});

const logger = log4js.getLogger();
const appDb = './sql/app.db';
instances = {};

module.exports = class Sqlite {
    constructor(dbPath) {
        this.dbPath = dbPath;
        this.db = new sqlite3.Database(dbPath || appDb, (err) => {
            if (err) {
                logger.error(err.message);
            }
            logger.log('Connected to the message queue database.');
        });
    }

    static connect(dbPath) {
        if (dbPath == null) dbPath = appDb;
        if (instances[dbPath] == null) {
            const result = new Sqlite(dbPath);
            instances[result.dbPath] = result;
            return result;
        } else {
            return instances[dbPath];
        }
    }

    async queryAsync(sql, params) {
        return new Promise((resolve, reject) => {
            this.db.all(sql, params, (err, rows) => {
                if (err) {
                    logger.error(err.message);
                    reject(err);
                } else {
                    resolve(rows);
                }
            });
        });
    }

    async getAsync(sql, params) {
        return new Promise((resolve, reject) => {
            this.db.get(sql, params, (err, data) => {
                if (err) {
                    logger.error(err.message);
                    reject(err);
                } else {
                    resolve(data);
                }
            });
        });
    }

    async run(sql, params) {
        return new Promise((resolve, reject) => {
            this.db.run(sql, params, (err) => {
                if (err) {
                    logger.error(err.message);
                    reject(err);
                } else {
                    resolve(true);
                }
            });
        });
    }

    close() {
        this.db.close();
    }
}
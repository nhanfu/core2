const Sqlite = require('../sql/sqlite');
const uuid = require('uuid');
const http = require('http');
const https = require('https');
const log4js = require('log4js');

log4js.configure({
    appenders: { out: { type: "file", filename: "out.log" } },
    categories: { default: { appenders: ["cheese"], level: "error" } },
});
const logger = log4js.getLogger();
var queueInstace = null;

module.exports = class MessageQueue {
    constructor(dbFilePath) {
        this.db = Sqlite.connect(dbFilePath);
        this.createTable();
        this.retry();
    }

    instance() {
        if (queueInstace === null) {
            queueInstace = new Queue();
        }
        return queueInstace;
    }

    createTable() {
        const sql = `
            CREATE TABLE IF NOT EXISTS Messages (
                id TEXT PRIMARY KEY,
                message TEXT,
                subscriberId TEXT,
                queueName TEXT
            )
            CREATE TABLE IF NOT EXISTS Subscribers (
                id TEXT PRIMARY KEY,
                endpoint TEXT,
                queueName TEXT
            )`;
        return this.db.run(sql);
    }

    async addAndNotifyAsync(queueName, message) {
        const messsageId = uuid.v4();
        const sql = 'INSERT INTO messages (id, message, queueName) VALUES (?, ?, ?)';
        await this.db.run(sql, [messsageId, message, queueName]);
        const subscribers = [];
        this.db.each('SELECT * FROM subscribers WHERE queueName = ?', queueName, async (err, target) => {
            subscribers.push(target);
            try {
                await this.trySendMessageAsync(target.endpoint, queueName, message, messsageId);
            }
            catch (err) {
                const sql = 'INSERT INTO messages (id, message, subscriberId, queueName) VALUES (?, ?, ?)';
                await this.db.run(sql, [messsageId, message, target.subscriberId, queueName]);
            }
        });
    }

    retry() {
        setInterval(() => {
            const failMessages = `select m.*, s.EndPoint from Messages m join Subscribers s on m.SubscriberId = s.Id`;
            this.db.each(failMessages, async (err, item) => {
                await this.trySendMessageAsync(target.endpoint, item.queueName, item.message, item.id);
                await this.deleteMessage(item.id);
            });
        }, 5000);
    }

    async getMessage(id) {
        const sql = 'SELECT * FROM messages WHERE id = ?';
        return await this.queryAsync(sql, id);
    }

    async deleteMessage(id) {
        const sql = 'DELETE FROM messages WHERE id = ?';
        return await this.db.run(sql, id);
    }

    addSubscriber(queueName, endpoint) {
        const id = uuid.v4();
        const sql = 'INSERT INTO subscribers (id, endpoint, queueName) VALUES (?, ?, ?)';
        return this.db.run(sql, [id, endpoint, queueName]);
    }

    getSubscriber(id) {
        const sql = 'SELECT * FROM subscribers WHERE id = ?';
        return this.db.queryAsync(sql, id);
    }

    async deleteSubscriber(id) {
        const sql = 'DELETE FROM subscribers WHERE id = ?';
        return await this.db.run(sql, id);
    }

    async trySendMessageAsync(endpoint, message, messageId) {
        try {
            const parsedUrl = url.parse(endpoint);
            switch (parsedUrl.protocol) {
                case 'http:':
                case 'https:':
                    await this.sendHttpMessageAysnc(endpoint, message);
                case 'tcp:':
                    await this.sendTcpMessageAync(endpoint, message);
                default:
                    logger.error(`Unsupported protocol: ${parsedUrl.protocol}`);
                    break;
            }
            logger.info(`Send message success to ${endpoint}, the message id is ${messageId}`)
        } catch (error) {
            logger.error(`Fail to send message to ${endpoint}, the message id is ${messageId}`);
        }
    }

    async sendHttpMessageAysnc(endpoint, message) {
        return new Promise((res, reject) => {
            const parsedUrl = url.parse(endpoint);
            const options = {
                hostname: parsedUrl.hostname,
                port: parsedUrl.port,
                path: parsedUrl.path,
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Content-Length': Buffer.byteLength(message),
                },
            };
            const client = parsedUrl.protocol === 'https:' ? https : http;
            const request = client.request(options, (response) => {
                let body = [];
                response.on('data', (data) => {
                    body.push(data);
                });
                response.on('end', (error) => {
                    const success = response.statusCode <= 300 && response.statusCode >= 200;
                    if (success) {
                        res(body.toString());
                    } else {
                        reject(error);
                    }
                });
                response.on('error', (error) => {
                    reject(error);
                });
            });
            request.write(typeof (message) === 'string' ? message : JSON.stringify({ message }));
            request.end();
        });
    }

    async sendTcpMessageAync(endpoint, message) {
        return new Promise((res, reject) => {
            const [host, port] = endpoint.split(':');
            const client = net.createConnection(port, host, () => {
                client.write(JSON.stringify({ message }));
                client.end();
            });
            client.on('data', (data) => {
                if (data) {
                    res(data);
                } else {
                    reject(new Error('No response'));
                }
            });
            client.on('error', (error) => {
                reject(error);
            });
        });
    }
}
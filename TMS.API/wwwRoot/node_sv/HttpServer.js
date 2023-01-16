var instance = null;

module.exports = class HttpServer {
    constructor() {
        this.http = require('http');
        this.url = require('url');
        this.LoadBalanceServer = require('./LoadBalancer');
        this.SvRunner = require('./svRunner');
        this.db = require('./sql/sqlite').connect();
        this.svRunner = new this.SvRunner();
        this.loadBalanceServer = new this.LoadBalanceServer();
        this.svRunner.run();
        this.loadBalanceServer.run();
    }

    static autoStart() {
        if (instance == null) {
            instance = new HttpServer();
        }
        instance.start();
        return instance;
    }

    start() {
        const requestListener = (r, s) => {
            const uri = this.url.parse(r.url);
            let body = [];

            r.on('data', (chunk) => {
                body.push(chunk);
            });

            r.on('end', async () => {
                try {
                    const arg = this.tryParseBody(body);
                    const path = arg && arg.path || uri.pathname;
                    if (path && path != '/') {
                        const result = await this.invokeService(path, arg, s);
                        this.writeStream(s, 200, typeof(result) === 'string' ? result : JSON.stringify(result));
                    }
                    else {
                        s.writeHead(200);
                        s.end('Ok');
                    }
                } catch (e) {
                    this.writeStream(s, e.status || 500, JSON.stringify({ message: e.message }));
                }
            });
        };

        this.server = this.http.createServer(requestListener);
        this.db.getAsync(`select * from [Const] where [Key] = 'host'`).then(row => {
            const conf = JSON.parse(row.Value);
            this.server.listen(conf.port, conf.host, () => {
                console.log(`Server is running on http://${conf.host}:${conf.port}`);
            });
        });
    }

    async invokeService(path, arg, s) {
        if (path == null) throw new Error('Path is required');
        const fn = require('./Services/' + path);
        if (fn == null) {
            const err = new Error('Service not found');
            err.status = 404;
            throw err;
        }
        const res = fn.call(this, arg == null ? null : arg.entity || arg.Entity, arg);
        const final = res instanceof Promise ? (await res) : typeof (res) == 'string' ? res : JSON.stringify(res) || 'Ok';
        return final;
    }

    writeStream(s, status, content, headers) {
        headers = headers || {
            'Access-Control-Allow-Origin': '*',
        };
        s.writeHead(status, headers);
        s.write(content);
        s.end();
    }

    tryParseBody(body) {
        if (body == null || body.length == 0) {
            return null;
        }
        try {
            return JSON.parse(body.toString().replace(/'/g, '\\\''));
        }
        catch (e) {
            return null;
        }
    }

    stop() {
    }
}
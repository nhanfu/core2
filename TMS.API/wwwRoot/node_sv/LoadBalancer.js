module.exports = class LoadBalanceServer {
    constructor() {
        this.sqlite3 = require('sqlite3').verbose();
        this.db = new this.sqlite3.Database('./sql/app.db');
        this.http = require('http');
        this.currentServerIndex = 0;
        this.targetServers = [];
    }

    run() {
        try {
            //  this.insertHost();
            this.createBalancerServer();
            this.createProxyServer();
        } catch (e) {
            console.log(e);
        }
    }

    createBalancerServer() {
        let loadBalanceServer = this.http.createServer((req, res) => {
            res.setHeader('Access-Control-Allow-Origin', '*');
            this.loadBalance(req, res);
        })
        let port = 8080;
        loadBalanceServer.listen(port, () => {
            console.log(`Load balancer listening on port ${port}`);
        })
    }

    createProxyServer() {
        this.db.all(`SELECT * FROM Servers`, (err, rows) => {
            if (err) {
                console.error(err.message);
            } else {
                this.targetServers = rows.map((row) => ({ host: row.host, port: row.port }));
                this.targetServers.forEach((serverData) => {
                    let server = this.http.createServer((req, res) => {
                        if (req.url === '/haha') {
                            res.writeHead(200, { 'Content-Type': 'text/plain' });
                            res.end(`Response from port ${serverData.port}`);
                        } else {
                            res.writeHead(404, { 'Content-Type': 'text/plain' });
                            res.end('404 Not Found');
                        }
                    });
                    server.listen(serverData.port, () => {
                        console.log(`Proxy server listening on port ${serverData.port}`);
                    });
                });
            }
        });
    }

    loadBalance(req, res) {
        let targetServer = this.targetServers[this.currentServerIndex];
        console.log(targetServer);
        this.currentServerIndex = (this.currentServerIndex + 1) % this.targetServers.length;

        let options = {
            host: targetServer.host,
            port: targetServer.port,
            path: req.url,
            method: req.method,
            headers: req.headers
        };

        let proxyReq = this.http.request(options, (proxyRes) => {
            res.writeHead(proxyRes.statusCode, proxyRes.headers);
            proxyRes.pipe(res);
        });
        req.pipe(proxyReq);
    }

    insertHost() {
        let host = 'localhost';
        let defaultPort = 3000;
        for (let index = 2; index < 100; index++) {
            let port = defaultPort + index + 1;
            this.db.run(`INSERT INTO Servers (host, port) VALUES (?, ?)`, [host, port], function (err) {
                if (err) {
                    console.error(err.message);
                } else {
                    console.log(`Successfully inserted ${host}:${port} into the database.`);
                }
            });
        }
    }
}
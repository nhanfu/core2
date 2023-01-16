module.exports = class SvRunner {
    constructor() {
        this.vm = require('vm');
        this.sqlite3 = require('sqlite3').verbose();
        this.log4js = require('log4js');
        this.config = require('./config.js');
        this.db = new this.sqlite3.Database(this.config.edi.sqlite);
        this.logger = this.log4js.getLogger();
        this.logger.level = "debug";
        this.serviceMap = {};
    }

    async run() {
        try {
            const query = `select * from [Services] where Active = 1 and IsServer = 1 and Interval > 0`
            this.db.each(query, (err, row) => {
                if (err) return;
                this.bgSvHanlder(row);
            });
        } catch (e) {
            console.log(e);
        }
    }

    async update(service) {
        this.bgSvHanlder(service);
    }

    bgSvHanlder(service) {
        if (service == null || service.Id == null) return;
        let meta = this.serviceMap[service.Id];

        if (meta == null) {
            meta = {};
            this.serviceMap[service.Id] = meta;
            meta.origin = service;
            if (service.IsServer && service.Active && service.Interval > 0) this.executeCode(meta, service);
            return;
        }

        const oldSv = meta.origin;
        if (oldSv != null && (!service.Active || !service.IsServer || service.Interval === 0 || service.Message == 'delete')) {
            clearInterval(meta.intervalId);
            delete this.serviceMap[service.Id];
        } else if (oldSv.Interval !== service.Interval) {
            clearInterval(meta.intervalId);
            this.executeCode(meta, service);
        }
    }

    getRequireMod(service) {
        if (service.Require == null) return null;
        return service.Require.split(',').map(mod => require(mod));
    }

    executeCode(meta, service) {
        const fn = this.vm.runInNewContext(service.Code);
        meta.intervalId = setInterval(async () => {
            try {
                const result = fn.call(null, this.getRequireMod(service));
                if (result instanceof Promise) {
                    await result;
                }
            } catch (err) {
                this.logger.error(`Error occurs while executing the service ${service.Id}\n${err}`);
            }
        }, service.Interval);
    }
}
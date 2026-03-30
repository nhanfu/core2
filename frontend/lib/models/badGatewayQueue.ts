export class BadGatewayQueue {
    constructor() {
        this._queue = [];
    }

    enqueue(options) {
        if (!options.NoQueue && options.method !== 'GET') {
            options.Retry = true;
            this._queue.push(options);
        }
    }

    dequeue() {
        return this._queue.shift();
    }

    peek() {
        return this._queue[0];
    }

    get count() {
        return this._queue.length;
    }
}

export { BadGatewayQueue as badGatewayQueue };

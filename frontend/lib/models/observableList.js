export class ObservableList {
    constructor(data = []) {
        this._data = data;
        this.listeners = [];
    }

    clear() {
        while (this._data.length) this._data.pop();
    }

    // Subscribe to changes
    subscribe(callback) {
        this.listeners.push(callback);
    }

    // Unsubscribe from changes
    unsubscribe(callback) {
        this.listeners = this.listeners.filter(listener => listener !== callback);
    }

    // Notify all listeners
    notify(action, item, index) {
        const args = { listData: this._data, item, index, action };
        this.listeners.forEach(listener => listener(args));
    }

    get data() {
        return this._data;
    }

    set data(value) {
        this._data = value;
        this.notify('Render');
    }

    add(item, index = this._data.length) {
        this._data.splice(index, 0, item);
        this.notify('Add', item, index);
    }

    remove(item) {
        const index = this._data.indexOf(item);
        if (index > -1) {
            this._data.splice(index, 1);
            this.notify('Remove', item, index);
        }
    }

    removeAt(index) {
        if (index >= 0 && index < this._data.length) {
            const item = this._data[index];
            this._data.splice(index, 1);
            this.notify('Remove', item, index);
        }
    }

    update(item, index) {
        if (index >= 0 && index < this._data.length) {
            this._data[index] = item;
            this.notify('Update', item, index);
        }
    }
}

class ObservableList {
    constructor(Data = []) {
        this._Data = Data;
        this.Listeners = [];
    }

    // Subscribe to changes
    Subscribe(callback) {
        this.Listeners.push(callback);
    }

    // Unsubscribe from changes
    Unsubscribe(Callback) {
        this.Listeners = this.Listeners.filter(listener => listener !== Callback);
    }

    // Notify all listeners
    Notify(Action, Item, Index) {
        const Args = { ListData: this._Data, Item, Index, Action };
        this.Listeners.forEach(Listener => Listener(Args));
    }

    get Data() {
        return this._Data;
    }

    set Data(Value) {
        this._Data = Value;
        this.Notify('Render');
    }

    Add(Item, Index = this._Data.length) {
        this._Data.splice(Index, 0, Item);
        this.Notify('Add', Item, Index);
    }

    Remove(Item) {
        const Index = this._Data.indexOf(Item);
        if (Index > -1) {
            this._Data.splice(Index, 1);
            this.Notify('Remove', Item, Index);
        }
    }

    RemoveAt(Index) {
        if (Index >= 0 && Index < this._Data.length) {
            const Item = this._Data[Index];
            this._Data.splice(Index, 1);
            this.Notify('Remove', Item, Index);
        }
    }

    Update(Item, Index) {
        if (Index >= 0 && Index < this._Data.length) {
            this._Data[Index] = Item;
            this.Notify('Update', Item, Index);
        }
    }
}

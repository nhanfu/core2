import { v7 as uuidv7 } from 'uuid';
export class Uuid7 {
    static newGuid() {
        return "-" + this.guid();
    }
    static guid(asOfNs = null) {
        return uuidv7();
    }

    static id25(asOfNs = null) {
        return this.guid();
    }
}

import { FeaturePolicy } from "./featurePolicy.js";

export class SecurityVM extends FeaturePolicy {
    allPermission = false;
    /** @type {string[]} */
    recordIds = [];
    get strRecordIds() {
        return this.recordIds.map(x => `"${x}"`).join();
    }
    /** @type {FeaturePolicy[]} */
    featurePolicy = [];
}
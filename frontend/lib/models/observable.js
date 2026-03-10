/**  @typedef {import("../editableComponent.js").default} EditableComponent */

/** @class ObservableArgs */
export default class ObservableArgs {
    /** @type {string} -Event type */
    evType;
    /** @type {EditableComponent} -Event type */
    com;
    /** @type {any} */
    newData;
    /** @type {any} */
    oldData;
    /** @type {any} */
    newMatch;
    /** @type {any} */
    oldMatch;
    /** @type {any} */
    newEntity;
    /** @type {any} */
    oldEntity;
    /** @type {string} */
    fieldName;
}
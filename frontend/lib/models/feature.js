import { Component } from "./component";
import { UserSetting } from "./userSetting";

/**
 * Represents a feature.
 * @class
 */
export class Feature {
    /** @type {string} */
    id = '';
    /** @type {string} */
    name = '';
    /** @type {string} */
    label = '';
    /** @type {string} */
    parentId = '';
    /** @type {number} */
    order = 0;
    /** @type {string} */
    className = '';
    /** @type {string} */
    style = '';
    /** @type {string} */
    styleSheet = '';
    /** @type {string} */
    script = '';
    /** @type {string} */
    events = '';
    /** @type {string} */
    icon = '';
    /** @type {boolean} */
    isDivider = false;
    /** @type {boolean} */
    isGroup = false;
    /** @type {boolean} */
    isMenu = false;
    /** @type {boolean} */
    isPublic = false;
    /** @type {boolean} */
    startUp = false;
    /** @type {string} */
    viewClass = '';
    /** @type {string} */
    entityId = '';
    /** @type {string} */
    description = '';
    /** @type {boolean} */
    active = false;
    /** @type {Date} */
    insertedDate = new Date();
    /** @type {string} */
    insertedBy = '';
    /** @type {Date} */
    updatedDate = new Date();
    /** @type {string} */
    updatedBy = '';
    /** @type {boolean} */
    isSystem = false;
    /** @type {boolean} */
    ignoreEncode = false;
    /** @type {boolean} */
    inheritParentFeature = false;
    /** @type {boolean} */
    deleteTemp = false;
    /** @type {boolean} */
    customNextCell = false;
    /** @type {boolean} */
    isFullScreen = false;
    /** @type {boolean} */
    isSmallScreen = false;
    /** @type {boolean} */
    loadEntity = false;
    /** @type {boolean} */
    isLock = false;
    /** @type {string} */
    html = '';
    /** @type {Component[]} */
    components = [];
    /** @type {any} */
    layout;
    /** @type {HTMLElement} */
    parentElement;
    /** @type {Feature[]} */
    inverseParent = [];
    /** @type {UserSetting[]} */
    userSettings = [];
}
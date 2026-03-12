import { SecurityVM } from "../models/securityVM.js";
import { TabEditor } from "../tabEditor.js";

export class SecurityBL extends TabEditor {
    /**
     * Initializes a new instance of the SecurityBL class.
     */
    constructor() {
        super("FeaturePolicy");
        this.Popup = true;
        this.Name = "securityEditor";
        this.Title = "Permissions";
        this.Icon = "mif-security";
    }

    /**
     * Gets the Security view model entity.
     * @returns {SecurityVM | null} The security entity if it exists.
     */
    get Security() {
        return this.Entity instanceof SecurityVM ? this.Entity : null;
    }
}

export class SecurityEditorBL extends TabEditor {
    /**
     * Initializes a new instance of the SecurityEditorBL class.
     */
    constructor() {
        super("FeaturePolicy");
        this.Popup = true;
        this.Name = "createSecurity";
        this.Title = "Permissions";
        this.Icon = "mif-security";
        document.addEventListener("dOMContentLoaded", this.checkAllPolicy.bind(this));
    }

    /**
     * @type {SecurityVM | null} The security entity
     */
    // @ts-ignore
    Entity;

    /**
     * Sets all permissions based on the allPermission flag.
     */
    checkAllPolicy() {
        const security = this.Entity;
        if (security) {
            security.canDelete = security.allPermission;
            security.canDeactivate = security.allPermission;
            security.canRead = security.allPermission;
            security.canWrite = security.allPermission;
            security.canShare = security.allPermission;
            this.findComponentByName("Properties").updateView();
        }
    }

    /**
     * Checks if all permissions are true to set allPermission flag.
     */
    checkPolicy() {
        const security = this.Entity;
        if (security) {
            security.allPermission = security.canDeactivate && security.canDelete && security.canRead && security.canShare && security.canWrite;
            this.findComponentByName("Properties").updateView();
        }
    }
}

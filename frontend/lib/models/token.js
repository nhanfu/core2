import { Client } from "../clients/client.js";

/**
 * Represents an authentication token with user details and permissions.
 */
export class Token {
    /** @type {string|null} User's unique identifier */
    userId = null;
    /** @type {string|null} Identifier for the user's cost center */
    tenantCode = null;
    /** @type {string|null} User's username */
    userName = null;
    /** @type {string|null} User's username */
    departmentId = null;
    /** @type {string|null} User's email address */
    email = null;
    /** @type {string|null} User's first name */
    firstName = null;
    /** @type {string|null} User's last name */
    lastName = null;
    /** @type {string|null} User's full name */
    fullName = null;
    /** @type {string|null} User's address */
    address = null;
    /** @type {string|null} URL to the user's avatar image */
    avatar = null;
    /** @type {string|null} Access token for user authentication */
    accessToken = null;
    /** @type {string|null} Refresh token for renewing the access token */
    refreshToken = null;
    /**
     * Expiration time of the access token.
     * @type {Date|null}
     */
    accessTokenExp = null;
    /**
     * Expiration time of the refresh token.
     * @type {Date|null}
     */
    refreshTokenExp = null;
    /** @type {string|null} Hashed password for the user */
    hashPassword = null;
    /** @type {string|null} Recovery token for password reset */
    recovery = null;
    /**
     * Vendor information associated with the user.
     * @type {any}
     */
    vendor = null;
    /** @type {Array<string>|null} List of role identifiers for the user */
    roleIds = [];
    /** @type {Array<string>|null} List of role names for the user */
    roleNames = [];
    /** @type {Array<string>|null} List of center identifiers for the user */
    centerIds = [];
    /** @type {string|null} User's social security number */
    ssn = null;
    /** @type {string|null} User's phone number */
    phoneNumber = null;
    /** @type {string|null} Identifier for the user's team */
    teamId = null;
    /** @type {string|null} Identifier for the user's partner entity */
    partnerId = null;
    /** @type {string|null} Identifier for the user's regional entity */
    regionId = null;
    /** @type {object|null} Additional arbitrary data associated with the user */
    additional = null;
    /**
     * Date and time the user signed in.
     * @type {Date|null}
     */
    signinDate = new Date();
    /** @type {string|null} Environment context for the user session */
    env = null;
    /** @type {string|null} Connection key used for database connections */
    connKey = null;
    systemRole = false;
    userAuthorization = [];
    constructor() {
        // Default values can be initialized here if different from null or empty.
        this.tenantCode = Client.Tenant; // Assuming Client.Tenant is accessible
        this.env = Client.Env;          // Assuming Client.Env is accessible
        this.connKey = Client.MetaConn; // Assuming Client.MetaConn is accessible
    }
    /**
     * @param {string} token
     */
    static parse(token) {
        var base64Url = token.split('.')[1];
        var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        const data = JSON.parse(jsonPayload);
        return data;
    }
}

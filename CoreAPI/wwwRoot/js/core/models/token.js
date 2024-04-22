import { Client } from "../clients/client.js";

/**
 * Represents an authentication token with user details and permissions.
 */
export class Token {
    /** @type {string|null} User's unique identifier */
    UserId = null;

    /** @type {string|null} Identifier for the user's cost center */
    CostCenterId = null;

    /** @type {string|null} User's username */
    UserName = null;

    /** @type {string|null} User's email address */
    Email = null;

    /** @type {string|null} User's first name */
    FirstName = null;

    /** @type {string|null} User's last name */
    LastName = null;

    /** @type {string|null} User's full name */
    FullName = null;

    /** @type {string|null} User's address */
    Address = null;

    /** @type {string|null} URL to the user's avatar image */
    Avatar = null;

    /** @type {string|null} Access token for user authentication */
    AccessToken = null;

    /** @type {string|null} Refresh token for renewing the access token */
    RefreshToken = null;

    /**
     * Expiration time of the access token.
     * @type {Date|null}
     */
    AccessTokenExp = new Date();

    /**
     * Expiration time of the refresh token.
     * @type {Date|null}
     */
    RefreshTokenExp = new Date();

    /** @type {string|null} Hashed password for the user */
    HashPassword = null;

    /** @type {string|null} Recovery token for password reset */
    Recovery = null;

    /**
     * Vendor information associated with the user.
     * @type {Vendor|null}
     */
    Vendor = null;

    /** @type {Array<string>|null} List of role identifiers for the user */
    RoleIds = [];

    /** @type {Array<string>|null} List of role names for the user */
    RoleNames = [];

    /** @type {Array<string>|null} List of center identifiers for the user */
    CenterIds = [];

    /** @type {string|null} User's social security number */
    Ssn = null;

    /** @type {string|null} User's phone number */
    PhoneNumber = null;

    /** @type {string|null} Identifier for the user's team */
    TeamId = null;

    /** @type {string|null} Identifier for the user's partner entity */
    PartnerId = null;

    /** @type {string|null} Identifier for the user's regional entity */
    RegionId = null;

    /** @type {object|null} Additional arbitrary data associated with the user */
    Additional = null;

    /**
     * Date and time the user signed in.
     * @type {Date|null}
     */
    SigninDate = new Date();

    /** @type {string|null} Tenant code for the user's tenant */
    TenantCode = null;

    /** @type {string|null} Environment context for the user session */
    Env = null;

    /** @type {string|null} Connection key used for database connections */
    ConnKey = null;

    constructor() {
        // Default values can be initialized here if different from null or empty.
        this.TenantCode = Client.Tenant; // Assuming Client.Tenant is accessible
        this.Env = Client.Env;          // Assuming Client.Env is accessible
        this.ConnKey = Client.MetaConn; // Assuming Client.MetaConn is accessible
    }
}

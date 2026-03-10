import { Client } from "../clients/client.js";

/**
 * Represents an email view model that encapsulates the email data.
 */
export class EmailVM {
    /**
     * Initializes a new instance of the EmailVM class with default values.
     */
    constructor() {
        /** @type {string} Connection key, default value fetched from Client.MetaConn */
        this.connKey = Client.MetaConn;

        /** @type {string} Email from address */
        this.fromAddress = '';

        /** @type {Array<string>} List of email addresses to send the email to */
        this.toAddresses = [];

        /** @type {Array<string>} List of email addresses for CC */
        this.cc = [];

        /** @type {Array<string>} List of email addresses for BCC */
        this.bcc = [];

        /** @type {string} Subject of the email */
        this.subject = '';

        /** @type {string} Body content of the email */
        this.body = '';

        /** @type {Array<string>} Texts that will be converted to PDF */
        this.pdfText = [];

        /** @type {Array<string>} Names of the files */
        this.fileName = [];

        /** @type {Set<string>} Collection of attachment identifiers */
        this.attachements = new Set();
    }
}
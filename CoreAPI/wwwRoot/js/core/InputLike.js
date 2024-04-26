import Textbox from './textbox.js';


class Textarea extends Textbox {
     /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.MultipleLine = true;
    }
}

class Password extends Textbox {
     /**
     * @param {Component} ui
     * @param {HTMLElement} [ele=null] 
     */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.Password = true;
    }
}

window.Core2 = window.Core || {};
window.Core2.Textarea = Textarea;
window.Core2.Password = Password;

export { Textarea, Password };

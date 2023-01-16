import { Form } from '../../../ngin/form.js';
import { meta } from './login.meta-v0.0.1.js';

class Login extends Form {
    constructor() {
        super(meta);
        this.render();
    }
}

new Login();
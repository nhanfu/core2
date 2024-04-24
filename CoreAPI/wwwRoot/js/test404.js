import { Label } from "./core/label.js";
import { Component } from "./core/models/component.js";
import { ValidationRule } from "./core/models/validationRule.js";
import { NumberBox } from "./core/numbox.js";
import { Html } from "./core/utils/html.js";

/** @type {Component} */
var labelMeta = {
    FieldName: 'test',
    Renderer: (x) => {
        Html.Take(x.Element).Div.Text('123');
    }
};
var a = new Label(labelMeta, document.getElementById('test'));
a.Render();

/** @type {Component} */
var numberMeta = {
    FieldName: 'test2',
    Validation: [{ Rule: ValidationRule.Required, Message: 'This is required'}]
};
var b = new NumberBox(numberMeta, document.getElementById('test2'));
b.Render();
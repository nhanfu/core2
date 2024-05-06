import { Datepicker } from './core/datepicker.js';
import { Chart } from './core/chart.js';
import { EditForm } from './core/editForm.js';
import { Component } from './core/models/component.js';

window.EditForm = EditForm;
var a = new Datepicker({
    FieldName: 'test',
}, document.getElementById('test'));
a.Render();

/** @type {Component} */
var meta = {
    FieldName: 'test2',
    LocalData: [
        {
          "name": "WR1",
          "y": 417,
          "startAngle": 25,
          "toolTipContent": "<b>{name}</b>: {y}",
          "indexLabelFontSize": 16
        },
        {
          "name": "TV",
          "y": 577,
          "startAngle": 25,
          "toolTipContent": "<b>{name}</b>: {y}",
          "indexLabelFontSize": 16
        }
      ]
};
var b = new Chart(meta, document.getElementById('test2'));
b.Render();
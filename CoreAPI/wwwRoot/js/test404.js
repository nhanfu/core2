import { Datepicker } from './core/datepicker.js';
import { Chart } from './core/chart.js';
import { EditForm } from './core/editForm.js';
import { Component } from './core/models/component.js';
import { ObservableList } from './core/models/observableList.js';
import { GridView } from './core/gridView.js';

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

let gvMeta = {
  Id: 'testGridView',
  IsSumary: true,
  IsCollapsible: false,
  CanSearch: true,
  ComponentType: 'GridView',
  Editable: true,
  CanAdd: true,
  DefaultAddStart: { HasValue: false },
  DefaultAddEnd: { HasValue: false },
  PopulateField: '',
  Events: {},
  Label: 'Test GridView',
  IsRealtime: false,
  CanCache: true,
  LocalRender: false,
  TopEmpty: false,
};
var gridView = new GridView(gvMeta);
gridView.Meta.LocalRender = true;
gridView.RowData = new ObservableList([{ Name: 'Test', Email: 'Join@gmail.com' }, { Name: 'Test2', Email: 'nhan@gmail.com' }]);;
gridView.Header = [ 
  { FieldName: 'Name', Label: 'Name', ComponentType: 'Textbox' },
  { FieldName: 'Email', Label: 'Email', ComponentType: 'Textbox' },
];
gridView.ParentElement = document.body;
gridView.Render();
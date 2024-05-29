import { Datepicker } from './core/datepicker.js';
import { Chart } from './core/chart.js';
import { ListView } from '../js/core/listView.js';
import { EditForm } from './core/editForm.js';
import { Component } from './core/models/component.js';
import { ObservableList } from './core/models/observableList.js';
import { GridView } from './core/gridView.js';
import { Button } from './core/button.js'
import { Checkbox } from './core/checkbox.js'
import { CodeEditor } from './core/codeEditor.js'
import { CompareGridView } from './core/compareGridView.js'
import { ContextMenu, ContextMenuItem } from './core/contextMenu.js';
import { Image } from './core/image.js';
import { Label } from './core/label.js';
import { NumBox } from './core/numbox.js';
// import { QRCode } from './core/qrcode.js';
import { SearchEntry } from './core/searchEntry.js';

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


var buttonElement = document.getElementById('test3');
var buttonMeta = {
  Id: 'testButton',
  ClassName: 'btn-primary',
  Label: 'Click Me',
  Style: 'width: 100px; height: 50px;',
  Events: {
    click: () => { alert('Button clicked!'); }
  }
};
var button = new Button(buttonMeta, buttonElement);
button.Render();


var checkboxElement = document.getElementById('test4');
var checkboxMeta = {
  Id: 'testCheckbox',
  ClassName: 'checkbox',
  Label: 'Check Me',
  Editable: true,
  Events: {
    change: (e) => { console.log('Checkbox changed!', e); }
  }
};
var checkbox = new Checkbox(checkboxMeta, checkboxElement);
checkbox.Render();


var codeEditorElement = document.getElementById('test5');
var codeEditorMeta = {
  Id: 'testCodeEditor',
  Lang: 'javascript',
  Theme: 'vs-light',
};
var codeEditor = new CodeEditor(codeEditorMeta, codeEditorElement);
codeEditor.Render();


// var compareGridViewElement = document.getElementById('test6');
// var compareGridViewMeta = {
//   Id: 'compareGridView',
//   Label: 'Compare GridView',
//   IsSumary: true,
//   IsCollapsible: false,
//   CanSearch: true,
//   ComponentType: 'GridView',
//   Editable: true,
//   CanAdd: true,
//   DefaultAddStart: { HasValue: false },
//   DefaultAddEnd: { HasValue: false },
//   PopulateField: '',
//   Events: {},
//   IsRealtime: false,
//   CanCache: true,
//   LocalRender: false,
//   TopEmpty: false,
// };

// var compareGridView = new CompareGridView(compareGridViewMeta);
// compareGridView.ParentElement = compareGridViewElement;
// compareGridView.RowData = [
//   { InsertedBy: 'User1', InsertedDate: '2024-05-29 10:00', ReasonOfChange: 'Initial Entry', TextHistory: 'Created new entry.' },
//   { InsertedBy: 'User2', InsertedDate: '2024-05-29 11:00', ReasonOfChange: 'Update', TextHistory: 'Updated entry details.' },
// ];
// compareGridView.Render();

// var contextMenuElement = document.getElementById('test7');

// var contextMenuMeta = {
//   Id: 'contextMenu',
//   Label: 'Context Menu',
//   IsSumary: true,
//   IsCollapsible: false,
//   CanSearch: true,
//   ComponentType: 'ContextMenu',
//   Editable: true,
//   CanAdd: true,
//   DefaultAddStart: { HasValue: false },
//   DefaultAddEnd: { HasValue: false },
//   PopulateField: '',
//   Events: {},
//   IsRealtime: false,
//   CanCache: true,
//   LocalRender: false,
//   TopEmpty: false,
// };

// var contextMenu = new ContextMenu(contextMenuMeta);
// contextMenu.ParentElement = contextMenuElement;
// contextMenu.MenuItems = [
//   {
//     Text: 'Option 1',
//     Icon: 'fa fa-option-1',
//     Click: () => { alert('Option 1 clicked'); },
//   },
//   {
//     Text: 'Option 2',
//     Icon: 'fa fa-option-2',
//     Click: () => { alert('Option 2 clicked'); },
//   },
//   {
//     Text: 'Option 3',
//     Icon: 'fa fa-option-3',
//     Click: () => { alert('Option 3 clicked'); },
//     MenuItems: [
//       {
//         Text: 'Sub Option 1',
//         Icon: 'fa fa-sub-option-1',
//         Click: () => { alert('Sub Option 1 clicked'); },
//       },
//       {
//         Text: 'Sub Option 2',
//         Icon: 'fa fa-sub-option-2',
//         Click: () => { alert('Sub Option 2 clicked'); },
//       },
//     ]
//   }
// ];

// contextMenu.Render();

var labelElement = document.getElementById('test9');
var labelMeta = {
  Id: 'labelComponent',
  ComponentType: 'Label',
  Label: 'Test Label',
  TextAlign: 'center',
  SimpleText: true,
  FormatEntity: null,
  Renderer: null,
  PreQuery: false,
  Editable: false,
  Events: {
    click: (e) => { console.log('Label clicked!', e); }
  }
};
var labelComponent = new Label(labelMeta);
labelComponent.ParentElement = labelElement;
labelComponent.Entity = { labelComponent: "This is a test label." }; 
labelComponent.Name = 'labelComponent';
labelComponent.Render();


// var numboxContainer = document.createElement('div');
// numboxContainer.setAttribute('id', 'numboxContainer');
// document.body.appendChild(numboxContainer); 
// var numboxElement = document.createElement('input'); 
// numboxElement.setAttribute('id', 'testNumBox');
// numboxContainer.appendChild(numboxElement);  
// var numboxMeta = {
//     Id: 'testNumBox',
//     ComponentType: 'NumBox',
//     Label: 'Enter Number',
//     Precision: 2,
//     Editable: true,
//     DefaultValue: 0,
//     Events: {
//         change: (e) => { console.log('NumBox value changed!', e); },
//         input: (e) => { console.log('NumBox input value!', e); }
//     }
// };
// var numBox = new NumBox(numboxMeta, numboxElement);
// numBox.Render();

// let qrMeta = {
//   Id: 'testQRCode',
//   Width: 128 
// };
// let qrElement = document.getElementById('qrCodeContainer');
// let qrCode = new QRCode(qrMeta, qrElement);
// qrCode.Render();

var searchEntryElement = document.getElementById('test10');
var searchEntryMeta = {
    FieldText: 'Name',
    LocalQuery: '[{"Id":1,"Name":"Option 1"},{"Id":2,"Name":"Option 2"},{"Id":3,"Name":"Option 3"}]', // Example data
    PlainText: 'Search...',
    HideGrid: false,
    Events: {
        change: (e) => { console.log('SearchEntry changed!', e); }
    }
};
var searchEntry = new SearchEntry(searchEntryMeta, searchEntryElement);
searchEntry.Render();


var imageElement = document.getElementById('test8');
var imageMeta = {
  Id: 'imageComponent',
  ComponentType: 'Image',
  Label: 'Image Upload',
  Template: 'image/*',
  Precision: 1,
  ChildStyle: { width: '100px', height: '100px', objectFit: 'cover' },
  Editable: true,
  Events: {
    change: (e) => { console.log('Image changed!', e); }
  }
};
var imageComponent = new Image(imageMeta);
imageComponent.ParentElement = imageElement;
imageComponent.Render();


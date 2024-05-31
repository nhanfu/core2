import { Datepicker } from './core/datepicker.js';
import { Chart } from './core/chart.js';
import { ListView } from '../js/core/listView.js';
import { EditForm } from './core/editForm.js';
import { Component } from './core/models/component.js';
import { ObservableList } from './core/models/observableList.js';
import { GridView } from './core/gridView.js';
// import { CompareGridView } from './core/compareGridView.js'
import { ContextMenu, ContextMenuItem } from './core/contextMenu.js';

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


import { Button } from './core/button.js'
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


import { Checkbox } from './core/checkbox.js'
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


import { CodeEditor } from './core/codeEditor.js'
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


// import { QRCodeCom } from './core/qrcode.js';
// var qrCodeElement = document.getElementById('test7');
// var qrCodeMeta = {
//   Id: 'qrCodeComponent',
//   ComponentType: 'QRCodeCom',
//   Width: 200,  
// };
// var qrCodeComponent = new QRCodeCom(qrCodeMeta, qrCodeElement);
// qrCodeComponent.ParentElement = qrCodeElement;
// qrCodeComponent.Entity = { qrCodeComponent: "https://www.google.com/" }; 
// qrCodeComponent.Name = 'qrCodeComponent'; 
// qrCodeComponent.Render();


import { ButtonPdf } from './core/buttonPdf.js';
var buttonElement = document.getElementById('test8');
var buttonPdfMeta = {
  Id: 'buttonPdfComponent',
  ComponentType: 'ButtonPdf',
  Label: 'Generate PDF',
  PlainText: 'Preview PDF',
  ClassName: 'btn btn-primary',
  Style: 'width: 120px; height: 40px;',
  Precision: 2,
  Events: {
    click: () => { console.log('Button PDF clicked!'); }
  }
};
var buttonPdfComponent = new ButtonPdf(buttonPdfMeta, buttonElement);
buttonPdfComponent.ParentElement = buttonElement;
buttonPdfComponent.Render();


// import { Textbox } from './core/textbox.js';
// var textboxElement = document.getElementById('test9');
// var textboxMeta = {
//   Id: 'textboxComponent',
//   ComponentType: 'Textbox',
//   PlainText: 'Enter text here...',
//   ShowLabel: true,
//   Events: {
//     input: () => { console.log('Textbox input event'); },
//     change: () => { console.log('Textbox change event'); }
//   }
// };
// var textboxComponent = new Textbox(textboxMeta, textboxElement);
// textboxComponent.ParentElement = textboxElement;
// textboxComponent.Entity = { textboxComponent: "Sample text" };
// textboxComponent.Name = 'textboxComponent'; 
// textboxComponent.Render();


// import { Rating } from './core/rating.js';
// var ratingElement = document.getElementById('test10');
// var ratingMeta = {
//   Id: 'ratingComponent',
//   ComponentType: 'Rating',
//   Precision: 5,  
//   Style: 'margin-right: 5px;', 
//   Events: {
//     click: (entity) => { console.log('Rating clicked!', entity); }
//   }
// };
// var ratingComponent = new Rating(ratingMeta, ratingElement);
// ratingComponent.ParentElement = ratingElement;
// ratingComponent.Entity = { ratingComponent: 3 };
// ratingComponent.Name = 'ratingComponent';
// ratingComponent.Render();


// import { Paginator, PaginationOptions } from './core/paginator.js';
// var tabGroupElement = document.getElementById('test11');
// var paginationOptions = new PaginationOptions(
//   100, 
//   10,  
//   1,  
//   0,  
//   1, 
//   10,
//   1,   
//   10,  
//   (pageIndex, event) => { console.log('Page changed to:', pageIndex); }
// );
// var paginatorComponent = new Paginator(paginationOptions);
// paginatorComponent.ParentElement = tabGroupElement;
// paginatorComponent.Render();



import { NumBox } from './core/numbox.js';
var numboxElement = document.getElementById('test19');
var numboxMeta = {
  Id: 'numboxComponent',
  ComponentType: 'NumBox',
  PlainText: 'Enter a number...',
  ShowLabel: true,
  Precision: 2, 
  Events: {
    input: () => { console.log('NumBox input event'); },
    change: () => { console.log('NumBox change event'); }
  }
};
var numboxComponent = new NumBox(numboxMeta, numboxElement);
numboxComponent.ParentElement = numboxElement;
numboxComponent.Entity = { numboxComponent: 123.45 }; 
numboxComponent.Name = 'numboxComponent'; 
numboxComponent.Render();


// import { Label } from './core/label.js';
// var labelElement = document.getElementById('test20');
// var labelMeta = {
//   Id: 'labelComponent',
//   ComponentType: 'Label',
//   Label: 'Test Label',
//   TextAlign: 'center',
//   SimpleText: true,
//   FormatEntity: null,
//   Renderer: null,
//   PreQuery: false,
//   Editable: false,
//   Events: {
//     click: (e) => { console.log('Label clicked!', e); }
//   }
// };
// var labelComponent = new Label(labelMeta);
// labelComponent.ParentElement = labelElement;
// labelComponent.Entity = { labelComponent: "This is a test label." }; 
// labelComponent.Name = 'labelComponent';
// labelComponent.Render();


// import { Image } from './core/image.js';
// var imageElement = document.getElementById('test21');
// var imageMeta = {
//   Id: 'imageComponent',
//   ComponentType: 'Image',
//   Label: 'Image Upload',
//   Template: 'image/*',
//   Precision: 1,
//   ChildStyle: { width: '100px', height: '100px', objectFit: 'cover' },
//   Editable: true,
//   Events: {
//     change: (e) => { console.log('Image changed!', e); }
//   }
// };
// var imageComponent = new Image(imageMeta);
// imageComponent.ParentElement = imageElement;
// imageComponent.Render();


import { SearchEntry } from './core/searchEntry.js';
var searchEntryElement = document.getElementById('testSearchEntry');
var searchEntryMeta = {
    FieldText: 'Name',
    LocalQuery: [{"Id":1,"Name":"Option 1"},{"Id":2,"Name":"Option 2"},{"Id":3,"Name":"Option 3"}], // Example data
    PlainText: 'Search...',
    HideGrid: false,
    Events: {
        change: (e) => { console.log('SearchEntry changed!', e); }
    }
};
var searchEntry = new SearchEntry(searchEntryMeta, null);
searchEntry.ParentElement = searchEntryElement;
searchEntry.Render();


import { TabGroup, TabComponent } from './core/tabComponent.js';
var tabGroupElement = document.getElementById('test23');
var tabGroupMeta = {
  Id: 'tabGroupComponent',
  ComponentType: 'TabGroup',
  IsVertialTab: false,
  Children: [
    {
      Id: 'tab1',
      FieldName: 'Tab 1',
      ComponentType: 'TabComponent',
      Label: 'Tab 1',
      Icon: 'fa fa-home',
      Description: 'This is tab 1',
      Children: [
      ]
    },
    {
      Id: 'tab2',
      FieldName: 'Tab 2',
      ComponentType: 'TabComponent',
      Label: 'Tab 2',
      Icon: 'fa fa-user',
      Description: 'This is tab 2',
      Children: [
      ]
    }
  ]
};
var tabGroupComponent = new TabGroup();
tabGroupComponent.Meta = tabGroupMeta;
tabGroupComponent.ParentElement = tabGroupElement;
tabGroupComponent.Render();
console.log(tabGroupMeta.Children);
tabGroupMeta.Children.forEach(childMeta => {
  var tabComponent = new TabComponent(childMeta);
  tabComponent.Parent = tabGroupComponent;
  tabComponent.Render();
  tabGroupComponent.Children.push(tabComponent);
});

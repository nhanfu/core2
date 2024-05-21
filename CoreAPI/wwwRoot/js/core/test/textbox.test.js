import { ElementType } from '../models/elementType.js';
import { Component } from '../models/component.js';
import {Textbox} from '../textbox';  // Adjust the import path as necessary
import { Utils } from "../utils/utils.js";
import { Client } from '../clients/client.js';

// Mock utilities to simplify behavior during tests
Utils.GetPropValue = jest.fn((entity, fieldName) => entity[fieldName]);
Utils.FormatEntity = jest.fn((format, entity) => JSON.stringify(entity));
Utils.DecodeSpecialChar = jest.fn(val => val);
Utils.EncodeSpecialChar = jest.fn(val => val);

describe('Textbox', () => {
    /** @type {Textbox} */
  let textbox;
  /** @type {HTMLInputElement} */
  let element;
  /** @type {Component} */
  let meta;
  /** @type {Obj} */
  let entity;

  beforeEach(() => {
    element = document.createElement('input');
    element.type = 'text';
    meta = { FieldName: 'testField', PlainText: 'Enter text', FormatData: '', Events: [], ShowLabel: true };
    textbox = new Textbox(meta, element);
    entity = { testField: 'Initial Value' };
    textbox.Entity = entity;
    textbox.EditForm = { Meta: { IgnoreEncode: false } };
    textbox.Render();
});

  test('constructor initializes properties correctly', () => {
    expect(textbox.Meta).toBe(meta);
    expect(textbox.Input).toBe(element);
    expect(textbox.Text).toBe('');  
    expect(textbox.Value).toBeNull();
});

  test('Text getter and setter works correctly', () => {
    textbox.Text = 'New Text';
    expect(textbox.Text).toBe('New Text');
    expect(textbox.Input.value).toBe('New Text');
  });

  test('Value getter and setter works correctly', () => {
    textbox.Value = 'New Value';
    expect(textbox.Value).toBe('New Value');
    expect(textbox.Text).toBe('New Value');
    expect(textbox.Input.value).toBe('New Value');
  });

  test('Render sets up input element correctly', () => {
    expect(textbox.Input).toBeDefined();
    expect(textbox.Input.value).toBe('Initial Value');
});

  test('ValidateUnique handles uniqueness check correctly', async () => {
    textbox.ValidationRules = { Unique: { Message: 'Must be unique' } };
    Utils.IsFunction = jest.fn(() => null);
    Client.Instance.ComQuery = jest.fn(() => Promise.resolve([]));

    const result = await textbox.ValidateUnique();
    expect(result).toBeTruthy();
    expect(Client.Instance.ComQuery).toHaveBeenCalled();
  });

  test('SetDisableUI disables and enables input correctly', () => {
    textbox.SetDisableUI(true);
    expect(textbox.Input.readOnly).toBe(true);

    textbox.SetDisableUI(false);
    expect(textbox.Input.readOnly).toBe(false);
  });
});
  


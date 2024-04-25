import { Component } from '../models/component.js';
import { ComponentType } from '../models/componentType.js';
import Textbox from '../textbox';  // Adjust the import path as necessary
import { Utils } from "../utils/utils.js";

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
    element = document.createElement(ComponentType.Input);  // Adjust for specific tests if necessary
    element.type = ComponentType.Input;
    meta = { FieldName: 'testField', PlainText: 'Enter text', FormatData: '', Events: [], ShowLabel: true };
    textbox = new Textbox(meta, element);
    entity = { testField: 'Initial Value' };
    textbox.Entity = entity;
    textbox.EditForm = { Meta: { IgnoreEncode: false } };
    textbox.Render();
  });

//   test('PopulateUIChange updates internal state correctly', () => {
//     // Mocking internal method and event setup
//     element.value = 'Changed Value';
//     textbox.Input = element;  // Ensure it's the same element we're manipulating
//     textbox.PopulateUIChange('input');
//     expect(textbox._text).toBe('Changed Value');
//     expect(textbox._oldText).toBe('Initial Value');
//   });

//   test('validateRegEx correctly validates user input', () => {
//     textbox.ValidationRules = {
//       RegEx: { RejectInvalid: true, Message: 'Invalid format', Test: jest.fn().mockImplementation(value => /^\d+$/.test(value)) }
//     };
//     // Valid input
//     expect(textbox.validateRegEx('12345', '^[0-9]+$')).toBeTruthy();
//     // Invalid input
//     expect(textbox.validateRegEx('abc123', '^[0-9]+$')).toBeFalsy();
//   });

//   test('ValidateAsync handles multiple validation rules', async () => {
//     // Setup multiple validation rules
//     textbox.ValidationRules = {
//       MinLength: { Min: 5 },
//       MaxLength: { Max: 10 },
//       RegEx: { Pattern: '^[a-z]+$' }
//     };

//     // Mock validation methods to simply check string length and regex pattern
//     textbox.Validate = jest.fn((rule, text, callback) => callback(text, textbox.ValidationRules[rule]));

//     const validationResult = await textbox.ValidateAsync();
//     expect(validationResult).toBeTruthy();
//     expect(textbox.Validate).toHaveBeenCalledTimes(3);
//   });

//   test('UpdateView does not update if not dirty', () => {
//     textbox.Dirty = false;
//     textbox.OldValue = 'Initial Value';
//     textbox.UpdateView();

//     expect(textbox._text).toBe('Initial Value');
//   });

  test('UpdateView forces update when dirty', () => {
    textbox.Dirty = true;
    textbox.Entity[textbox.FieldName] = 'New Value';
    textbox.UpdateView(true);

    expect(textbox._text).toBe('New Value');
    expect(textbox.OldValue).toBe('Initial Value'); // OldValue should NOT be updated
  });
});


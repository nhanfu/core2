import { Component } from '../models/component';
import { Checkbox } from '../checkbox'; // Adjust the import according to your project structure
import { ComponentType } from '../models/componentType';
import { ElementType } from '../models/elementType.js';

describe('Checkbox', () => {
    /** @type {Checkbox} */
    let checkbox;
    /** @type {hTMLInputElement} */
    let mockElement;
    /** @type {Component} */
    let mockMeta;

    beforeEach(() => {
        mockElement = document.createElement(ElementType.input);
        mockElement.type = 'checkbox'
        mockMeta = { Editable: true, Events: [], fieldName: 'testField' };
        checkbox = new Checkbox(mockMeta, mockElement);
    });

    test('constructor should initialize properties correctly', () => {
        expect(checkbox.Meta).toBe(mockMeta);
        expect(checkbox.defaultValue).toBe(false);
        expect(checkbox._value).toBeNull();
        expect(checkbox._input).toBe(mockElement);
    });

    test('Render should create input element and bind events', () => {
    checkbox.render();
    expect(checkbox._input).toBeDefined();
    expect(checkbox.element === checkbox._input || checkbox.element === checkbox._input.parentElement).toBe(true);
    expect(checkbox._input.type).toBe('checkbox');
});

    test('userChange should prevent default if disabled', () => {
        const mockEvent = { preventDefault: jest.fn() };
        checkbox.Disabled = true;
        checkbox.userChange(mockEvent);
        expect(mockEvent.preventDefault).toHaveBeenCalled();
    });

    test('userChange should update value and trigger dataChanged', () => {
        checkbox.render();
        const spyDataChanged = jest.spyOn(checkbox, 'dataChanged');
        checkbox._input.checked = true;
        const mockEvent = new Event('input');
        checkbox.userChange(mockEvent);
        expect(spyDataChanged).toHaveBeenCalledWith(true);
        expect(checkbox._value).toBe(true);
    });

    test('dataChanged should set new value and mark as dirty', () => {
        checkbox.render();
        checkbox.dataChanged(true);
        expect(checkbox._value).toBe(true);
        expect(checkbox.Dirty).toBe(true);
    });

    test('setDisableUI should disable or enable the input element based on argument', () => {
        checkbox.render();
        checkbox.setDisableUI(true);
        expect(checkbox._input.disabled).toBe(true);
        checkbox.setDisableUI(false);
        expect(checkbox._input.disabled).toBe(false);
    });

    test('updateView should refresh the value from the entity', () => {
        checkbox.Entity[checkbox.Name] = true;
        //checkbox.render();
        checkbox.updateView();
        expect(checkbox._input.checked).toBe(true);
    });
});


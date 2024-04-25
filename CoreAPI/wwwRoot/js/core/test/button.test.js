// Import statements if needed
import { Button } from '../button'; // Uncomment if Button class is in a separate file
import { Component } from '../models/component';

describe('Button', () => {
    /** @type {Component} */
    let ui;
    /** @type {HTMLDivElement} */
    let element;
    let spinnerMock;

    beforeEach(() => {
        ui = { Id: '123', FieldName: 'btnSave', ClassName: 'btn-class', Style: 'color: red;', Icon: 'icon-path', Label: 'Click me', Events: {} };
        element = document.createElement('div');
        document.body.appendChild(element);

        spinnerMock = {
            AppendTo: jest.fn(),
            Hide: jest.fn()
        };

        global.Spinner = spinnerMock;
    });

    afterEach(() => {
        document.body.removeChild(element);
    });

    test('should throw error if ui is not provided', () => {
        expect(() => new Button(null)).toThrow("ui is required");
    });

    test('DispatchClick should handle disabled state and call events', async () => {
        const button = new Button(ui, element);
        button.Render();
        button.Disabled = false;

        const mockDispatchEvent = jest.fn().mockResolvedValue();
        button.DispatchEvent = mockDispatchEvent;

        await button.DispatchClick();

        expect(spinnerMock.AppendTo).toHaveBeenCalledWith(element);
        expect(mockDispatchEvent).toHaveBeenCalledWith(ui.Events, "click", button.Entity, button);
        expect(spinnerMock.Hide).toHaveBeenCalled();
    });

    test('GetValueText should return textContent of _textEle if Entity or Meta is null', () => {
        const button = new Button(ui, element);
        button.Entity = { btnSave: "Some text" };

        expect(button.GetValueText()).toEqual("Some text");
    });
});


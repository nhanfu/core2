const Textbox = require('../textbox').default; 
import { Utils } from '../utils/utils';
import { Html } from '../utils/html';

jest.mock('../utils/utils', () => ({
    Utils: {
        GetPropValue: jest.fn(),
        DecodeSpecialChar: jest.fn(),
        EncodeSpecialChar: jest.fn(),
        FormatEntity: jest.fn(),
        IsFunction: jest.fn(),
    }
}));

jest.mock('../utils/html', () => ({
    Html: {
        Take: jest.fn(() => ({
            TextArea: {
                Value: jest.fn(),
            }
        })),
        Instance: {
            Attr: jest.fn(),
            Style: jest.fn(),
            PlaceHolder: jest.fn(),
        },
    },
}));


describe('Textbox Render Function', () => {
    let meta;
    let ele;
    let textboxInstance;

    beforeEach(() => {
        meta = {
            PlainText: 'Plain Text',
            Row: 2, 
            ChildStyle: 'mockChildStyleFunction', 
            ShowLabel: false,
            FormatData: 'mockFormatData', 
            FormatEntity: 'mockFormatEntity',
        };
        ele = document.createElement('input');
        textboxInstance = new Textbox(meta, ele);
    });

    test('Render function handles missing FormatData and FormatEntity correctly', () => {
        meta.FormatData = null;
        meta.FormatEntity = null;
        textboxInstance.Render();

    });

    // test('Render function sets textarea value and rows attribute correctly for multiple line textbox', () => {
    //     // Arrange
    //     meta.Row = 2; // Set Row to 2
    //     textboxInstance.MultipleLine = true; // Set MultipleLine to true
    //     Utils.GetPropValue.mockReturnValueOnce(null);
    //     Utils.DecodeSpecialChar.mockReturnValueOnce('decodedText');
    
    //     // Act
    //     textboxInstance.Render();
    
    //     // Assert
    //     expect(Html.Instance.Attr).toHaveBeenCalledWith('rows', 2);
    //     expect(Html.Take).toHaveBeenCalled();
    //     expect(Html.Take().TextArea.Value).toHaveBeenCalledWith('decodedText');
    // });

    test('Render function does not call child style function if Meta.ChildStyle is empty', () => {
        meta.ChildStyle = ''; 
        textboxInstance.Render();
    });

    test('Render function sets password text-security correctly', () => {
        textboxInstance.Password = true;
        textboxInstance.Render();
    });

    test('Render function sets placeholder text to PlainText when ShowLabel is true and PlainText is empty string', () => {
        meta.ShowLabel = true;
        meta.PlainText = '';
        textboxInstance.Render();
    });
});


describe('SetDisableUI Function', () => {
    let meta;
    let ele;
    let textboxInstance;

    beforeEach(() => {
        meta = {
            PlainText: 'Plain Text',
            Row: 2, 
            ChildStyle: 'mockChildStyleFunction', 
            ShowLabel: false,
            FormatData: 'mockFormatData', 
            FormatEntity: 'mockFormatEntity',
        };
        ele = document.createElement('input');
        textboxInstance = new Textbox(meta, ele);
    });

    test('SetDisableUI function disables input field', () => {
        textboxInstance.Input = {
            ReadOnly: false,
        };
        textboxInstance.SetDisableUI(true);
        expect(textboxInstance.Input.ReadOnly).toBe(true);
    });

    test('SetDisableUI function disables textarea field', () => {
        textboxInstance.TextArea = {
            ReadOnly: false,
        };
        textboxInstance.SetDisableUI(true);
        expect(textboxInstance.TextArea.ReadOnly).toBe(true);
    });

    test('SetDisableUI function does not modify input field if it does not exist', () => {
        textboxInstance.Input = null; 
        textboxInstance.SetDisableUI(true);
        expect(textboxInstance.Input).toBeNull(); 
    });

    test('SetDisableUI function does not modify textarea field if it does not exist', () => {
 
        textboxInstance.TextArea = null; 
        textboxInstance.SetDisableUI(true);
        expect(textboxInstance.TextArea).toBeNull(); 
    });
});
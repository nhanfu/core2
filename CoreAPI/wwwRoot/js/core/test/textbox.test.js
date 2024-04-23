const Textbox = require('../textbox').default; 

test('cộng 1 + 2 sẽ bằng 3', () => {
    const textboxInstance = new Textbox();

    expect(textboxInstance.sum(1, 2)).toBe(3);
});
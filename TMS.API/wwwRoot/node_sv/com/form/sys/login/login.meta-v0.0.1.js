const meta = [
    {
        com: '../button/button-v0.0.1.js',
        get label() { return 'Test'; },
        events: {
            click: (arg) => {
                console.log(`This is the message from meta, the label text should be ${arg.com.ele.innerText}`);
            }
        }
    },
    {
        com: '../button/button-v0.0.1.js',
        get label() { return 'Test2'; },
        events: {
            click: (arg) => {
                console.log(`This is another message`);
            }
        }
    },
    {
        com: '../input/input-v0.0.1.js',
        get val() { return 'user name' },
        events: {
            change: (arg) => {
                console.log(`Should login to the user ${arg.com.ele.value}`);
            }
        }
    }
]

export { meta }
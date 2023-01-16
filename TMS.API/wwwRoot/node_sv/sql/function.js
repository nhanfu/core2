const http = require('http');
var sqlite = require('sqlite-sync');

//Connecting - if the file does not exist it will be created
sqlite.connect('./sql/app.db');

//Updating - returns the number of rows modified - can be async too
var rows_modified = sqlite.update("COMPANY", { NAME: "TESTING UPDATE" }, { ID: 1 });

//Create your function
function test(a, b) {
    // notify to localhost itself
    const request = http.request({
        host: 'localhost',
        port: 8089,
        method: 'POST',
        headers: {
            'Content-Type': 'application/json; charset=utf-8'
        }
    }, (res) => {
        let data = [];

        res.on('data', x => {
            data.push(x);
        });
        res.on('end', async () => {
            console.log(data.toString());
        });
    });
    request.write(JSON.stringify({ a, b }));
    request.end();
    return a + b;
}

//Add your function to connection
sqlite.create_function(test);

// Use your function in the SQL
console.log(sqlite.run("SELECT ID, test(NAME, ' Inc ahihi') as NAME FROM COMPANY"));

// Closing connection 
sqlite.close();

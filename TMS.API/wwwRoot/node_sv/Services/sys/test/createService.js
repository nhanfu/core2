const Sqlite = require('../../../sql/sqlite');
const uuid = require('uuid');

module.exports = async function (service, args) {
    const id = uuid.v4();
    service.Id = id;
    const code = service.Code || `x => console.log('ahihi')`;
    const sql = `insert into [Services] (Id, Name, Code) values (?, 'Test', ?)`;
    await Sqlite.connect().run(sql, id, code);
    return service;
}
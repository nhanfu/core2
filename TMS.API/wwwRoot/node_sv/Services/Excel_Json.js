'use strict';

module.exports = (entity, args) => {
    const xlsx = require('xlsx');
    const path = require('path');

    const wb = xlsx.readFile(path.join(args.absPath.replace('\\', '/'), entity));
    var sheetName = wb.SheetNames[0];
    var sheet = wb.Sheets[sheetName];

    const data = xlsx.utils.sheet_to_json(sheet, { defval: "" });
    var res = { Result: JSON.stringify(data), SqlType: 3, System: 'KIOWAY' };
    return JSON.stringify(res);
}

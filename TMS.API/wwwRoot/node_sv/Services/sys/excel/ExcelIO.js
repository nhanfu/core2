const sql = require('mssql');
const xlsx = require('xlsx');

module.exports = class ExcelIO {
    constructor(outPath, inPath, connStr) {
        this.outPath = outPath;
        this.inPath = inPath;
        this.connStr = connStr;
    }

    exportSimpleJson(json) {
        const workSheet = xlsx.utils.json_to_sheet(json);
        const workBook = xlsx.utils.book_new();
        xlsx.utils.book_append_sheet(workBook, workSheet, "Sheet 1");
        xlsx.writeFile(workBook, this.outPath);
    }

    async parseExcel(sqlQuery, gridPolicy) {
        await sql.connect(this.connStr);
        const rows = await sql.query(sqlQuery);
        // 1. Load data
        // 1. Load master data
        const refFields = gridPolicy.filter(x => x.RefName != null);
        // const parentIds = rows.map(x => x.ParentId).join();
        // await refDataSet = sql.query(`select * from MasterData where Id in (${parentIds})`);
        // 2. 
    }
}
const sql = require('mssql');
const config = require('../../../../../config');

module.exports = async (x) => {
	await sql.connect(config.tms.wr1.sql);
	var skip = x.pSize * x.pIndex;
	var tables = x.term == null ? `from [MasterData] m1` :
		`from [MasterData] m1 left join [MasterData] m2 on m1.ParentId = m2.Id
		where m1.Name like @term or m2.Name like @term`;

	var query = `
		declare @term nvarchar(max) = '%${x.term}%';
		
		select m1.* 
		into #Collection
		${tables}
		order by Id
		offset ${skip} rows
		fetch next ${x.pSize} rows only
		
		select * from #Collection
		select count(m1.Id) as total
		${tables}
		
		select * from GridPolicy where FeatureId = 1054 and EntityId = 1067 and Active = 1
		
		select *, 'MasterData' as RefName from MasterData m1
		where Id in (select ParentId from #Collection)
		`;
	const data = await sql.query(query.toString());
	var res = {
		Query: query,
		Result: data,
		System: 'Default',
		SqlType: 1
	};
	return JSON.stringify(res);
}

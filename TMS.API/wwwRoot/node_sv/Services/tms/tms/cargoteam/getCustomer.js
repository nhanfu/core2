module.exports = (x) => {
	var entity = JSON.parse(x);
	var fromDate = entity.lastModified;
	cmd = `select PartnerID,PartnerName2,PartnerName3,Address,Address2,Taxcode
		from Partners
		where DateModify >= '${fromDate}' and [Group] = 'CUSTOMERS'`
	var res = JSON.stringify({
		Query: cmd,
		System: 'CARGOTEAM',
		SqlType: 1
	});
	return res;
}
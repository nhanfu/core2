'use strict';

module.exports = (entity, args) => {
	const patch = JSON.parse(entity);
	var table = patch.Entity;
	var changes = patch.Changes;
	var id = changes.filter(x => x.Field == 'Id')[0];
	var actualChange = changes.filter(x => x.Field != 'Id')[0];
	var update = `update [${table}] 
	set [${actualChange.Field}] = '${actualChange.Value}' 
	where Id = ${id.Value}`;
	var res = { Query: update, SqlType: 1, System: 'Default' };
	return JSON.stringify(res);
};
const utf8 = 'utf8';

function queueItemHandler(queItem, sql, key) {
  if (queItem.RecordId === null && queItem.Message == null) return;
  let client = http;
  if (queItem.Address != null && queItem.Address.indexOf('https') >= 0) {
    client = https;
  }

  const [ArrivedPOLDate, OnboardDate, ArrivedPODDate, ReleasedDate, PickedUpDate] = queItem.Message.split(',').map(x => x === '' ? null : new Date(x));
  const json = JSON.stringify({
    bill: queItem.RecordId, ArrivedPOLDate: ArrivedPOLDate, OnboardDate: OnboardDate, ArrivedPODDate: ArrivedPODDate,
    ReleasedDate: ReleasedDate, PickedUpDate: PickedUpDate
  });
  const options = makeRequestOptions(queItem.Address, 'PUT');
  options.headers['apiKey'] = key;
  const request = client.request(options, resp => queueMessageResponseHandler(resp, queItem, sql));
  request.write(json);
  request.end();
}

function makeRequestOptions(rawUrl, method) {
  const uri = url.parse(rawUrl);
  var post_options = {
    host: uri.host.replace(':' + uri.port, ''),
    port: uri.port,
    path: uri.path,
    method: method,
    headers: {
      'Content-Type': 'application/json; charset=utf-8'
    }
  };
  return post_options;
}

function queueMessageResponseHandler(res, message, sql) {
  res.setEncoding(utf8);
  let data = [];

  res.on('data', x => {
    data.push(x);
  });
  res.on('end', async () => {
    if (data.length == 0) return;
    const result = JSON.parse(data.toString());
    if (result.message != 'Ok') return;
    try {
      const query = `declare @id uniqueidentifier = convert(uniqueidentifier, '${message.Id}'); 
                    delete from [MessageQueue] where Id = @id`;
      const res = await sql.query(query);
      const [a, b] = res.rowsAffected;
      console.assert(a === 1 && b === 1, 'Should delete the item ' + message.Id);
    } catch (e) {
      console.error(e);
    }
  });
}

function signIn() {
  const signInOptions = makeRequestOptions(config.tms.wr1.portal.signIn, 'POST');
  return new Promise((sucess, fail) => {

    const client = https.request(signInOptions, res => {
      let data = [];
      res.on('data', x => {
        data.push(x);
      });
      res.on('end', () => {
        sucess(JSON.parse(data.toString()));
      });
    });
    const json = JSON.stringify({ username: config.tms.wr1.portal.username, password: config.tms.wr1.portal.password });
    client.write(json);
    client.end();
  });
}

async function execute() {
  try {
    await sql.connect(config.tms.wr1.fast.sqlConfig);
    const queue = await sql.query`select * from [MessageQueue] where [Name] = 'TransactionInfo'`;
    if (queue == null || queue.recordset == null || queue.recordset.length == 0) return;
    const key = await signIn();
    queue.recordset.forEach(message => queueItemHandler(message, sql, key.result.apiKey));
  } catch (e) {
    console.log(e);
  }
}

(modules) => {
  [sql, http, https, url, config] = modules;
  execute().then(() => console.log('bg sv of TMS is running'));
}
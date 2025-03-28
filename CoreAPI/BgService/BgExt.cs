using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Data;

namespace CoreAPI.BgService
{
    public static class BgExt
    {
        public static async Task<T[]> ReadDsAsArr<T>(string query, string connInfo = null) where T : class
        {
            var ds = await ReadDataSet(query, connInfo);
            if (ds.Length == 0 || ds[0].Length == 0) return [];
            return ds[0].Select(x => x.MapTo<T>()).ToArray();
        }

        public static string GetConnectionString(IServiceProvider serviceProvider, IConfiguration _configuration, string system)
        {
            string tenantCode = GetTanentCode(serviceProvider);
            var connectionStr = string.Empty;
            if (!tenantCode.IsNullOrWhiteSpace())
            {
                connectionStr = _configuration.GetConnectionString(tenantCode);
                if (connectionStr != null)
                {
                    return connectionStr;
                }
            }
            connectionStr = _configuration.GetConnectionString(system);
            if (!tenantCode.IsNullOrWhiteSpace())
            {
                connectionStr = connectionStr.Replace($"logistics_forwardx", $"logistics_{tenantCode}");
            }
            return connectionStr;
        }

        private static string GetTanentCode(IServiceProvider serviceProvider)
        {
            var httpContext = serviceProvider.GetService<IHttpContextAccessor>();
            string tenantCode = null;
            if (httpContext?.HttpContext is not null)
            {
                var claim = httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim);
                if (claim is not null)
                {
                    tenantCode = claim.Value.ToUpper();
                }
                if (tenantCode.IsNullOrWhiteSpace())
                {
                    tenantCode = httpContext.HttpContext.Request.Query["t"].ToString();
                }
            }
            return tenantCode;
        }

        public static object GetValueOrNull(this Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out object value) ? value : null;
        }

        public static async Task<Dictionary<string, object>[][]> ReadDataSet(string query, string connStr)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connStr);
            var sideEffect = HasSideEffect(query);
            if (sideEffect) throw new ApiException("Side effect of query is NOT allowed");
            var tables = new List<Dictionary<string, object>[]>();
            try
            {
                using (var con = new SqlConnection(connStr))
                using (var sqlCmd = new SqlCommand(query, con) { CommandType = CommandType.Text })
                {
                    await con.OpenAsync();
                    using (var reader = await sqlCmd.ExecuteReaderAsync())
                    {
                        do
                        {
                            var table = new List<Dictionary<string, object>>();
                            while (await reader.ReadAsync())
                            {
                                table.Add(ReadSqlRecord(reader));
                            }
                            tables.Add(table.ToArray());
                        }
                        while (await reader.NextResultAsync());
                    }
                }
                return tables.ToArray();
            }
            catch (Exception e)
            {
                var message = $"{e.Message} {query}";
                throw new ApiException(message, e)
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                };
            }
        }

        static readonly TSqlTokenType[] SideEffectCmd = [
        TSqlTokenType.Insert, TSqlTokenType.Update, TSqlTokenType.Delete,
            TSqlTokenType.Create, TSqlTokenType.Drop, TSqlTokenType.Alter,
            TSqlTokenType.Truncate, TSqlTokenType.MultilineComment, TSqlTokenType.SingleLineComment
        ];

        public static bool HasSideEffect(string sql, params TSqlTokenType[] allowCmds)
        {
            var finalCmd = SideEffectCmd.Except(allowCmds).ToArray();
            TSql110Parser parser = new(true);
            var fragments = parser.Parse(new StringReader(sql), out var errors);
            return fragments.ScriptTokenStream.Any(x => finalCmd.Contains(x.TokenType));
        }

        private static Dictionary<string, object> ReadSqlRecord(IDataRecord reader)
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader[i];
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }
            return row;
        }

        public static async Task<Dictionary<string, object>[][]> GetTableColumns(string tableName, string connStr)
        {
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            return await ReadDataSet(query, connStr);
        }

        public static async Task<SqlResult> SavePatch2(PatchVM vm, string connStr)
        {
            var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
            var tableColumns = (await GetTableColumns(vm.Table, connStr))[0];
            var filteredChanges = vm.Changes.Where(change => tableColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
            var selectIds = new List<DetailData>();
            if (id.StartsWith("-"))
            {
                id = id.Substring(1);
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        using (SqlCommand command = new SqlCommand())
                        {
                            command.Transaction = transaction;
                            command.Connection = connection;
                            var update = filteredChanges.Select(x => $"@{id.Replace("-", "") + x.Field.ToLower()}");
                            var cells = filteredChanges.Select(x => x.Field).ToList();
                            if (!vm.Delete.Nothing())
                            {
                                command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                            }
                            command.CommandText += $"INSERT into [{vm.Table}]([{cells.Combine("],[")}]) values({update.Combine()})";
                            foreach (var item in filteredChanges)
                            {
                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                {
                                    item.Value = item.Value.Substring(1);
                                }
                                command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                            }
                            int index = 1;
                            if (!vm.Detail.Nothing())
                            {
                                foreach (var detailArray in vm.Detail)
                                {
                                    foreach (var detail in detailArray)
                                    {
                                        var tableDetailColumns = (await GetTableColumns(detail.Table, connStr))[0];
                                        var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                                        var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                                        if (idDetail.StartsWith("-"))
                                        {
                                            var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                            var cellsDetails = filteredDetailChanges.Select(x => x.Field).ToList();
                                            command.CommandText += $";INSERT into [{detail.Table}]([{cellsDetails.Combine("],[")}]) values({updateDetail.Combine()})";
                                            foreach (var item in filteredDetailChanges)
                                            {
                                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                                {
                                                    item.Value = item.Value.Substring(1);
                                                }
                                                command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                            }
                                        }
                                        else
                                        {
                                            filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                            var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                            command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                            foreach (var item in filteredDetailChanges)
                                            {
                                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                                {
                                                    item.Value = item.Value.Substring(1);
                                                }
                                                command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                            }
                                        }
                                    }
                                    selectIds.Add(new DetailData()
                                    {
                                        Index = index,
                                        Table = detailArray[0].Table,
                                        ComId = detailArray[0].ComId,
                                        Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                                    });
                                    index++;
                                }
                            }
                            await command.ExecuteNonQueryAsync();
                            await transaction.CommitAsync();
                            await connection.CloseAsync();
                            var childs = new List<string>();
                            var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                            foreach (var item in selectIds)
                            {
                                sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                            }
                            var entity = await ReadDataSet(sql, connStr);
                            selectIds.ForEach(x =>
                            {
                                x.Data = entity[x.Index];
                            });
                            return new SqlResult()
                            {
                                updatedItem = entity[0],
                                Detail = selectIds,
                                status = 200,
                                message = "create successfull"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return new SqlResult()
                        {
                            updatedItem = null,
                            status = 500,
                            message = ex.Message
                        };
                    }
                }
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    await connection.OpenAsync();
                    SqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        using (SqlCommand command = new SqlCommand())
                        {
                            command.Transaction = transaction;
                            command.Connection = connection;
                            var updates = filteredChanges.Where(x => x.Field != "Id").ToList();
                            var update = updates.Select(x => $"[{x.Field}] = @{id.Replace("-", "") + x.Field.ToLower()}");
                            if (!vm.Delete.Nothing())
                            {
                                command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                            }
                            command.CommandText += $" UPDATE [{vm.Table}] SET {update.Combine()} WHERE Id = '{id}';";
                            foreach (var item in updates)
                            {
                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                {
                                    item.Value = item.Value.Substring(1);
                                }
                                command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                            }
                            var changes = updates.Where(x => !x.HistoryValue.IsNullOrWhiteSpace());
                            if (!changes.Nothing())
                            {
                                var history = changes.Select(x => x.HistoryValue).Combine("\n").Replace("'", "''");
                                if (!history.IsNullOrWhiteSpace())
                                {
                                    command.CommandText += $" INSERT INTO [History](Id,TextContent,RecordId,TableName,Active,InsertedDate,InsertedBy) values('{Uuid7.Guid()}',N'{history}','{id}','{vm.Table}',1,'{DateTime.Now.ToISOFormat()}','-1');";
                                }
                            }
                            int index = 1;
                            if (!vm.Detail.Nothing())
                            {
                                foreach (var detailArray in vm.Detail)
                                {
                                    foreach (var detail in detailArray)
                                    {
                                        var tableDetailColumns = (await GetTableColumns(detail.Table, connStr))[0];
                                        var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                                        var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                                        if (idDetail.StartsWith("-"))
                                        {
                                            var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                            var insertDetail = filteredDetailChanges.Select(x => $"[{x.Field}]").ToList();
                                            command.CommandText += $";INSERT into [{detail.Table}]({insertDetail.Combine()}) values({updateDetail.Combine()})";
                                            foreach (var item in filteredDetailChanges)
                                            {
                                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                                {
                                                    item.Value = item.Value.Substring(1);
                                                }
                                                command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                            }
                                        }
                                        else
                                        {
                                            filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                            var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                            command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                            foreach (var item in filteredDetailChanges)
                                            {
                                                if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                                {
                                                    item.Value = item.Value.Substring(1);
                                                }
                                                command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                            }
                                        }
                                    }
                                    selectIds.Add(new DetailData()
                                    {
                                        Index = index,
                                        Table = detailArray[0].Table,
                                        ComId = detailArray[0].ComId,
                                        Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                                    });
                                    index++;
                                }
                            }
                            await command.ExecuteNonQueryAsync();
                            await transaction.CommitAsync();
                            await connection.CloseAsync();
                            var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                            foreach (var item in selectIds)
                            {
                                sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                            }
                            var entity = await ReadDataSet(sql, connStr);
                            selectIds.ForEach(x =>
                            {
                                x.Data = entity[x.Index];
                            });
                            return new SqlResult()
                            {
                                updatedItem = entity[0],
                                Detail = selectIds,
                                status = 200,
                                message = "update successfull"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        var entity = await ReadDataSet($"SELECT * FROM [{vm.Table}] where Id = '{id}'", connStr);
                        return new SqlResult()
                        {
                            updatedItem = entity[0],
                            status = 500,
                            message = ex.Message
                        };
                    }
                }
            }
        }

        public static Task NotifyDevices(IEnumerable<TaskNotification> tasks, string queueName, WebSocketService _socket, string tenantCode)
        {
            return tasks
                .Where(x => x.AssignedId.HasAnyChar())
                .Select(x => new MQEvent
                {
                    QueueName = queueName,
                    Id = "-" + Uuid7.Guid().ToString(),
                    Message = x,
                    AssignedId = x.AssignedId
                })
            .ForEachAsync(x => SendMessageToUser(x, _socket, tenantCode));
        }

        private static async Task SendMessageToUser(MQEvent task, WebSocketService _socket, string tenantCode)
        {
            var env = "dev";
            var fcm = new FCMWrapper
            {
                To = $"/topics/{tenantCode}/{env}/U{task.AssignedId:0000000}",
                Data = new FCMData
                {
                    Title = task.Message.Title,
                    Body = task.Message.Description,
                },
                Notification = new FCMNotification
                {
                    Title = task.Message.Title,
                    Body = task.Message.Description,
                    ClickAction = "com.softek.tms.push.background.MESSAGING_EVENT"
                },
            };
            await _socket.SendMessageToUsersAsync([task.Message.AssignedId], task.ToJson(), fcm.ToJson(), tenantCode);
        }

        public static async Task<T> ReadDsAs<T>(string query, string connInfo = null) where T : class
        {
            var ds = await ReadDataSet(query, connInfo);
            if (ds.Length == 0 || ds[0].Length == 0) return null;
            return ds[0][0].MapTo<T>();
        }
    }
}

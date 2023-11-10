using ClosedXML.Excel;
using Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using Tenray.Topaz;
using Tenray.Topaz.API;
using Core.Exceptions;
using Core.Models;

namespace Core.Controllers
{
    [Authorize]
    public class TMSController<T> : GenericController<T> where T : class
    {
        protected readonly TMSContext db;
        protected readonly ILogger<TMSController<T>> _logger;
        protected HttpClient _client;
        protected string PathField = "Path";
        protected string ParentIdField = "ParentId";
        protected string ChildrenField = "InverseParent";
        private string _address;

        public TMSController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
            _logger = (ILogger<TMSController<T>>)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(ILogger<TMSController<T>>));
            _config = _httpContext.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            _address = _config["ServiceAddress"];
        }

        [HttpPost("api/listener/[Controller]")]
        public virtual Task<ActionResult<T>> CreateListenerAsync([FromBody] T entity)
        {
            return CreateAsync(entity);
        }

        [HttpPut("api/listener/[Controller]")]
        public virtual Task<ActionResult<T>> UpdateListenerAsync([FromBody] T entity)
        {
            return UpdateAsync(entity);
        }

        [HttpDelete("api/listener/[Controller]/{id}")]
        public virtual Task<ActionResult<bool>> HardDeleteListenerAsync([FromRoute] string id)
        {
            return HardDeleteAsync(id);
        }

        protected async Task<bool> HasSystemRole()
        {
            return await db.Role.AnyAsync(x => AllRoleIds.Contains(x.Id) && x.RoleName.Contains("system"));
        }


        protected virtual async Task Approving(T entity)
        {
            var oldEntity = await db.Set<T>().FindAsync((int)entity.GetComplexPropValue(IdField));
            oldEntity.CopyPropFrom(entity);
            await db.SaveChangesAsync();
        }

        public async Task<ActionResult<T>> UpdateTreeNodeAsync([FromBody] T entity, string reasonOfChange = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var res = await base.UpdateAsync(entity, reasonOfChange);
            var parentId = (int?)entity.GetPropValue(ParentIdField);
            var id = (int)entity.GetPropValue(IdField);
            var path = entity.GetPropValue(PathField) as string;
            var entityChildren = entity.GetPropValue(ChildrenField) as ICollection<T>;
            if (parentId != null)
            {
                var parentEntity = await db.Set<T>().FindAsync(parentId);
                var pathParent = parentEntity.GetPropValue(nameof(MasterData.Path));
                entity.SetPropValue(PathField, @$"\{pathParent}\{parentId}\".Replace(@"\\", @"\"));
            }
            else
            {
                entity.SetPropValue(PathField, null);
            }
            SetLevel(entity);
            var folderPath = "\\" + id + "\\";
            var query = $"select * from [{typeof(T).Name}] where charindex('{folderPath}', [Path]) >= 1 or Id = {id} or {ParentIdField} = {id} order by [Path] desc";
            var allNodes = await db.Set<T>().FromSqlRaw(query).ToListAsync();
            if (allNodes.Nothing())
            {
                return res;
            }
            var nodeMap = BuildTreeNode(allNodes);
            var directChildren = nodeMap.Values.Where(x => (int?)x.GetPropValue(ParentIdField) == id);
            SetChildrenPath(directChildren, nodeMap, new HashSet<T>() { entity });
            await db.SaveChangesAsync();
            return res;
        }

        private Dictionary<int, T> BuildTreeNode(List<T> allNode)
        {
            var nodeMap = allNode.Where(x => x != null).DistinctBy(x => (int)x.GetPropValue(IdField)).ToDictionary(x => (int)x.GetPropValue(IdField));
            nodeMap.Values.SelectForeach(x =>
            {
                var parentId = (int?)x.GetPropValue(ParentIdField);
                var id = (int)x.GetPropValue(IdField);
                var path = (string)x.GetPropValue(PathField);
                if (parentId is null || parentId is null || !nodeMap.ContainsKey(parentId.Value))
                {
                    return;
                }
                var parent = nodeMap[parentId.Value];
                x.SetPropValue(ParentIdField, parent.GetPropValue(IdField));
            });
            return nodeMap;
        }

        protected void SetChildrenPath(IEnumerable<T> directChildren, Dictionary<int, T> nodeMap, HashSet<T> visited)
        {
            if (directChildren.Nothing())
            {
                return;
            }
            directChildren.SelectForeach(child =>
            {
                var id = (int)child.GetPropValue(IdField);
                var parentId = child.GetPropValue(ParentIdField) as int?;
                if (parentId is not null && nodeMap.ContainsKey(parentId.Value))
                {
                    var parent = nodeMap[parentId.Value];
                    var parentPath = parent.GetPropValue(PathField) as string;
                    child.SetPropValue(PathField, @$"\{parentPath}\{parentId}\".Replace(@"\\", @"\"));
                }
                SetLevel(child);
                SetChildrenPath(nodeMap.Values.Where(x => (int?)x.GetPropValue(ParentIdField) == id), nodeMap, visited);
            });
        }

        protected void SetLevel(T entity)
        {
            var path = entity.GetPropValue(PathField) as string;
            entity.SetPropValue("Level", path.IsNullOrWhiteSpace() ? 0 : path.Split(@"\").Where(x => !x.IsNullOrWhiteSpace()).Count());
        }

        private void OrderHeaderGroup(List<Component> headers)
        {
            Component tmp;
            for (int i = 0; i < headers.Count - 1; i++)
            {
                for (int j = i + 2; j < headers.Count; j++)
                {
                    if (headers[i].GroupName.HasAnyChar()
                        && headers[i].GroupName == headers[j].GroupName
                        && headers[i + 1].GroupName != headers[j].GroupName)
                    {
                        tmp = headers[i + 1];
                        headers[i + 1] = headers[j];
                        headers[j] = tmp;
                    }
                }
            }
        }

        public string DecodeEntity(string entity)
        {
            switch (entity)
            {
                case "amp":
                    return "&";
                case "quot":
                    return "\"";
                case "gt":
                    return ">";
                case "lt":
                    return "<";
                case "nbsp":
                    return " ";
                default:
                    return entity;
            }
        }

        public string ConvertHtmlToPlainText(string htmlContent)
        {
            // Remove HTML tags using regular expression
            string plainText = Regex.Replace(htmlContent, @"<[^>]+>|&nbsp;", "").Trim();

            // Decode HTML entities using regular expression
            plainText = Regex.Replace(plainText, @"&(amp|quot|gt|lt|nbsp);", m => DecodeEntity(m.Groups[1].Value));

            return plainText;
        }

        [HttpGet("api/[Controller]/ExportExcel")]
        public async Task<string> ExportExcel([FromServices] IServiceProvider serviceProvider
            , [FromServices] IConfiguration config
            , [FromQuery] int componentId
            , [FromQuery] string sql
            , [FromQuery] string where
            , [FromQuery] bool custom
            , [FromQuery] string featureId
            , [FromQuery] string order
            , [FromQuery] bool showNull
            , [FromQuery] string orderby
            , [FromQuery] string join)
        {
            var component = await db.Component.FindAsync(componentId);
            var userSetting = await db.UserSetting.FirstOrDefaultAsync(x => x.Name == $"{(custom ? "Export" : "ListView")}-" + componentId && x.UserId == UserId);
            var gridPolicySys = new List<Component>();
            if (userSetting != null)
            {
                gridPolicySys = JsonConvert.DeserializeObject<List<Component>>(userSetting.Value);
            }
            var gridPolicy = await db.Component.Where(x => x.EntityId == component.ReferenceId && x.FeatureId == featureId && x.Active && !x.Hidden).ToListAsync();
            var specificComponent = gridPolicy.Any(x => x.ComponentId == component.Id);
            if (specificComponent)
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == component.Id).ToList();
            }
            else
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == null).ToList();
            }
            if (gridPolicySys != null)
            {
                var gridPolicys = new List<Component>();
                var userSettings = gridPolicySys.ToDictionary(x => x.Id);
                gridPolicy.ForEach(x =>
                {
                    var current = userSettings.GetValueOrDefault(x.Id);
                    if (current != null)
                    {
                        x.IsExport = current.IsExport;
                        x.Order = current.Order;
                        x.OrderExport = current.OrderExport;
                    }
                });
            }
            gridPolicy = gridPolicy.Where(x => x.ComponentType != "Button" && !x.ShortDesc.IsNullOrWhiteSpace() && ((custom && x.IsExport) || !custom)).OrderBy(x => custom ? x.OrderExport : x.Order).ToList().ToList();
            OrderHeaderGroup(gridPolicy);
            var reportQuery = string.Empty;
            var pros = typeof(T).GetProperties().Where(x => x.CanRead && x.PropertyType.IsSimple()).Select(x => x.Name).ToList();
            var selects = gridPolicy.Where(x => x.ComponentType == "Dropdown" && pros.Contains(x.FieldName)).ToList().Select(x =>
            {
                if (x.ExcelFieldName.IsNullOrWhiteSpace())
                {
                    var format = x.FormatData.Split("}")[0].Replace("{", "");
                    var objField = x.FieldName.Substring(0, x.FieldName.Length - 2);
                    return $"[{objField}].[{format}] as [{objField}]";
                }
                else
                {
                    return x.ExcelFieldName;
                }
            });
            var joins = gridPolicy.Where(x => x.ComponentType == "Dropdown").ToList().Select(x =>
            {
                var objField = x.FieldName.Substring(0, x.FieldName.Length - 2);
                return $"left join {(!x.DatabaseName.IsNullOrWhiteSpace() ? $"{x.DatabaseName}.dbo." : "")}[{x.RefName}] as [{objField}] on [{objField}].Id = [{component.RefName}].{x.FieldName}";
            }).Distinct().ToList();

            var idFields = gridPolicy.Where(x => x.ComponentType == "Dropdown").ToList().Select(x =>
            {
                return $"[{component.RefName}].{x.FieldName}";
            }).Distinct().ToList();
            var select1s = gridPolicy.Where(x => x.ComponentType != "Dropdown").Distinct().ToList().Select(x =>
            {
                if (x.ExcelFieldName.IsNullOrWhiteSpace())
                {
                    return $"[{component.RefName}].[{x.FieldName}]";
                }
                else
                {
                    return x.ExcelFieldName;
                }
            }).Distinct().ToList();
            var fieldNames = select1s.Union(selects.Union(idFields)).ToList();
            fieldNames.Add($"[{component.RefName}].Id");
            fieldNames = fieldNames.Distinct().ToList();
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {fieldNames.Combine()}
                                  from ({sql})  as [{component.RefName}]
                                  {join}
                                  {joins.Combine(" ")}
                                  where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery = $@"select {fieldNames.Combine()}
                                  from [{component.RefName}] as [{component.RefName}]
                                  {join}
                                  {joins.Combine(" ")}
                                  where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            if (!orderby.IsNullOrWhiteSpace() && !orderby.Contains(",Id desc") && !orderby.Contains(",Id asc") && !orderby.Contains($",[{component.RefName}].Id desc") && !orderby.Contains($",[{component.RefName}].Id asc") && orderby != "Id desc" && orderby != "Id asc" && orderby != $"[{component.RefName}].Id asc" && orderby != $"[{component.RefName}].Id desc")
            {
                reportQuery += $" order by {orderby},[{component.RefName}].Id asc";
            }
            else
            {
                if (orderby == $"[{component.RefName}].Id asc" || orderby == $"[{component.RefName}].Id desc")
                {
                    reportQuery += $" order by {orderby}";
                }
                else
                {
                    reportQuery += $" order by {orderby}".Replace(" Id desc", $" [{component.RefName}].Id desc").Replace(" Id asc", $" [{component.RefName}].Id asc");
                }
            }
            var s = $"";
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var sqlCmd = new SqlCommand(reportQuery, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            try
            {
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    do
                    {
                        var table = new List<Dictionary<string, object>>();
                        while (await reader.ReadAsync())
                        {
                            table.Add(Read(reader));
                        }
                        tables.Add(table);
                    } while (reader.NextResult());
                }
            }
            catch (Exception e)
            {
                throw new ApiException(e.Message);
            }
            bool anyGroup = gridPolicy.Any(x => !string.IsNullOrEmpty(x.GroupName));
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(typeof(T).Name);
            worksheet.Cell("A1").Value = component.Label.IsNullOrWhiteSpace() ? component.RefName : component.Label;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range(1, 1, gridPolicy.Count + 1, gridPolicy.Count + 1).Row(1).Merge();
            worksheet.Style.Font.SetFontName("Times New Roman");
            var i = 2;
            worksheet.Cell(2, 1).SetValue("STT");
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            if (anyGroup)
            {
                worksheet.Range(2, 1, 3, 1).Merge();
            }
            foreach (var item in gridPolicy)
            {
                if (anyGroup && !string.IsNullOrEmpty(item.GroupName))
                {
                    var colspan = gridPolicy.Count(x => x.GroupName == item.GroupName);
                    if (item != gridPolicy.FirstOrDefault(x => x.GroupName == item.GroupName))
                    {
                        i++;
                        continue;
                    }
                    worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(item.GroupName));
                    worksheet.Range(2, i, 2, i + colspan - 1).Merge();
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Font.Bold = true;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    i++;
                    continue;
                }
                worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(item.ShortDesc));
                worksheet.Cell(2, i).Style.Font.Bold = true;
                worksheet.Cell(2, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (anyGroup && string.IsNullOrEmpty(item.GroupName))
                {
                    worksheet.Range(2, i, 3, i).Merge();
                    worksheet.Cell(3, i).Style.Font.Bold = true;
                    worksheet.Cell(3, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(3, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                i++;
            }
            var h = 2;
            if (anyGroup)
            {
                foreach (var item in gridPolicy)
                {
                    if (anyGroup && !string.IsNullOrEmpty(item.GroupName))
                    {
                        worksheet.Cell(3, h).SetValue(ConvertHtmlToPlainText(item.ShortDesc));
                        worksheet.Cell(3, h).Style.Font.Bold = true;
                        worksheet.Cell(3, h).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(3, h).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                    h++;
                }
            }
            var x = 3;
            if (anyGroup)
            {
                x++;
            }
            var j = 1;
            foreach (var item in tables[0])
            {
                var y = 2;
                worksheet.Cell(x, 1).SetValue(j);
                worksheet.Cell(x, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                foreach (var itemDetail in gridPolicy)
                {
                    var vl = item.GetValueOrDefault(itemDetail.FieldName);
                    switch (itemDetail.ComponentType)
                    {
                        case "Input":
                            var vl1 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl1);
                            break;
                        case "Textarea":
                            var vl2 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl2);
                            break;
                        case "Label":
                            var vl3 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl3);
                            break;
                        case "Datepicker":
                            worksheet.Cell(x, y).SetValue((DateTime?)vl);
                            break;
                        case "Number":
                            if (vl is int)
                            {
                                worksheet.Cell(x, y).SetValue(vl is null ? default(int) : (int)vl);
                            }
                            else
                            {
                                worksheet.Cell(x, y).SetValue(vl is null ? default(decimal) : (decimal)vl);
                                worksheet.Cell(x, y).Style.NumberFormat.Format = "#,##";
                            }
                            break;
                        case "Dropdown":
                            var objField = itemDetail.FieldName.Substring(0, itemDetail.FieldName.Length - 2);
                            vl = item.GetValueOrDefault(objField);
                            var vl4 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl4);
                            break;
                        case "Checkbox":
                            worksheet.Cell(x, y).SetValue(vl.ToString() == "False" ? default(int) : 1);
                            break;
                        default:
                            break;
                    }
                    worksheet.Cell(x, y).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    y++;
                }
                j++;
                x++;
            }
            var k = 2;
            var last = tables[0].Count + 3;
            worksheet.Cell(last, 1).Value = "Total";
            worksheet.Cell(last, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            foreach (var item in gridPolicy)
            {
                if (item.ComponentType == "Number")
                {
                    var value = tables[0].Select(x => x[item.FieldName]).Where(x => x != null).Sum(x =>
                    {
                        if (x is int)
                        {
                            return x is null ? default(int) : (int)x;
                        }
                        else
                        {
                            return x is null ? default(decimal) : (decimal)x;
                        }
                    });
                    worksheet.Cell(last, k).SetValue(value);
                    worksheet.Cell(last, k).Style.Font.Bold = true;
                    worksheet.Cell(last, k).Style.NumberFormat.Format = "#,##";
                }
                worksheet.Cell(last, k).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                k++;
            }
            var url = $"{component.RefName}{DateTimeOffset.Now:ddMMyyyyhhmm}.xlsx";
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }

        [HttpPost("api/[Controller]/svc")]
        public async Task ExecuteJs([FromQuery] string svId, [FromQuery] string path, [FromBody] string param)
        {
            Models.Services sv = null;
            if (svId.HasAnyChar())
            {
                sv = await db.Services.FindAsync(svId);
            }
            else if (path.HasAnyChar())
            {
                var subPaths = path.Split("_");
                if (subPaths.Length < 3) return;
                var vendor = subPaths[0];
                var env = subPaths[1];
                var sub = subPaths[2];
                sv = await db.Services.FirstAsync(x => x.VendorName == vendor && x.Env == env && x.Path == sub);
            }
            var engine = new TopazEngine();
            engine.SetValue("JSON", new JSONObject());
            engine.AddType<HttpClient>("HttpClient");
            engine.AddNamespace("System");
            engine.SetValue("param", param);
            engine.SetValue("Response", Response);

            await engine.ExecuteScriptAsync(sv.Content);
            var res = engine.GetValue("result");
            var contentType = (string)engine.GetValue("contentType");
            Response.ContentType = sv.ResHeaders ?? contentType ?? "text/html";
            await Response.WriteAsync((string)res);
        }
    }
}

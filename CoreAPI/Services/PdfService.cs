using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using Elsa.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class PdfService
    {
        private readonly UserService _userService;
        public PdfService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<string> CreateHtml(CreateHtmlVM createHtmlVM, string conn)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", conn);
            var components = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and ComponentGroupId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var gridPolicys = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and EntityId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var sql = component.Query;
            if (createHtmlVM.Data.GetValueOrNull("TokenUserId") != null)
            {
                createHtmlVM.Data["TokenUserId"] = _userService.UserId;
            }
            else
            {
                createHtmlVM.Data.Add("TokenUserId", _userService.UserId);
            }
            if (createHtmlVM.Data.GetValueOrNull("TokenRoleNames") != null)
            {
                createHtmlVM.Data["TokenRoleNames"] = _userService.RoleNames.Combine() ?? string.Empty;
            }
            else
            {
                createHtmlVM.Data.Add("TokenRoleNames", _userService.RoleNames.Combine());
            }
            if (createHtmlVM.Data.GetValueOrNull("TokenPartnerId") != null)
            {
                createHtmlVM.Data["TokenPartnerId"] = _userService.VendorId ?? string.Empty;
            }
            else
            {
                createHtmlVM.Data.Add("TokenPartnerId", _userService.VendorId);
            }
            if (createHtmlVM.Data.GetValueOrNull("TokenUserName") != null)
            {
                createHtmlVM.Data["TokenUserName"] = _userService.UserName;
            }
            else
            {
                createHtmlVM.Data.Add("TokenUserName", _userService.UserName);
            }
            if (createHtmlVM.Data.GetValueOrNull("TokenGroupId") != null)
            {
                createHtmlVM.Data["TokenGroupId"] = _userService.GroupId ?? string.Empty;
            }
            else
            {
                createHtmlVM.Data.Add("TokenGroupId", _userService.GroupId);
            }
            if (!sql.IsNullOrWhiteSpace())
            {
                var sqlQuery = FormatString(sql, createHtmlVM.Data);
                var customData = await BgExt.ReadDataSet(sqlQuery, conn);
                foreach (var item in customData[0][0])
                {
                    if (createHtmlVM.Data.GetValueOrNull(item.Key) != null)
                    {
                        createHtmlVM.Data[item.Key] = item.Value;
                    }
                    else
                    {
                        createHtmlVM.Data.Add(item.Key, item.Value);
                    }
                }
                int i = 0;
                foreach (var row in customData.Skip(1).ToList())
                {
                    createHtmlVM.Data.Add("c" + i, row);
                    i++;
                }
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(component.Template);
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                ReplaceNode(createHtmlVM, item, dirCom);
            }
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                ReplaceTableNode(createHtmlVM, item, dirCom);
            }
            foreach (var item in document.DocumentNode.ChildNodes)
            {
                ReplaceCTableNode(createHtmlVM, item, dirCom);
            }
            return document.DocumentNode.InnerHtml;
        }

        private string FormatString(string html, Dictionary<string, object> data)
        {
            var dollarCurlyBraceRegex = new Regex(@"\{(.+?)\}");
            var matches = dollarCurlyBraceRegex.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var valueWithinCurlyBraces = match.Groups[1].Value;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(valueWithinCurlyBraces);
                    string plainText = htmlDoc.DocumentNode.InnerText;
                    html = html.Replace($"{{{valueWithinCurlyBraces}}}", data[plainText]?.ToString());
                }
            }
            return html;
        }

        private void ReplaceNode(CreateHtmlVM createHtmlVM, HtmlNode htmlNode, Dictionary<string, Component> dirCom)
        {
            var dollarCurlyBraceRegex = new Regex(@"\${(.+?)\}");
            var matches = dollarCurlyBraceRegex.Matches(htmlNode.InnerHtml);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var valueWithinCurlyBraces = match.Groups[1].Value;
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(valueWithinCurlyBraces);
                    string plainText = htmlDoc.DocumentNode.InnerText;
                    var curentComponent = dirCom.GetValueOrDefault(plainText);
                    if (curentComponent != null)
                    {
                        var currentData = createHtmlVM.Data[curentComponent.FieldName];
                        switch (curentComponent.ComponentType)
                        {
                            case "Datepicker":
                                currentData = currentData is null ? "" : Convert.ToDateTime(currentData).ToString("dd/MM/yyyy");
                                break;
                            case "Number":
                                currentData = currentData is null ? "" : Convert.ToDecimal(currentData).ToString(curentComponent.FormatData ?? "N0");
                                break;
                            case "Dropdown":
                                var displayField = curentComponent.FieldName;
                                var containId = curentComponent.FieldName.EndsWith("Id");
                                if (containId)
                                {
                                    displayField = displayField.Substring(0, displayField.Length - 2);
                                }
                                else
                                {
                                    displayField = displayField + "MasterData";
                                }
                                currentData = FormatString(curentComponent.FormatData, JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(createHtmlVM.Data[displayField])));
                                break;
                            default:
                                break;
                        }
                        htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        if (plainText.Contains("."))
                        {
                            var fields = plainText.Split(".");
                            var mainObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(createHtmlVM.Data[fields[0]]));
                            var currentData = mainObject[fields[1]];
                            htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                        }
                        else
                        {
                            var currentData = createHtmlVM.Data[plainText];
                            htmlNode.InnerHtml = htmlNode.InnerHtml.Replace($"${{{valueWithinCurlyBraces}}}", currentData?.ToString() ?? string.Empty);
                        }
                    }
                }
            }
            foreach (var item in htmlNode.ChildNodes)
            {
                ReplaceNode(createHtmlVM, item, dirCom);
            }
        }

        private HtmlNode FindClosest(HtmlNode node, string element)
        {
            while (node != null && node.Name != element)
            {
                node = node.ParentNode;
            }
            return node;
        }

        private void ProcessPlaceholders(HtmlNode node, Regex regex, Dictionary<string, object> data)
        {
            var matches = regex.Matches(node.InnerHtml);
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;
                if (data[placeholder] != null)
                {
                    node.InnerHtml = node.InnerHtml.Replace($"#{{{placeholder}}}", data[placeholder]?.ToString() ?? string.Empty);
                }
                else
                {
                    node.InnerHtml = node.InnerHtml.Replace($"#{{{placeholder}}}", string.Empty);
                }
            }
        }

        private void ProcessPlaceholders2(HtmlNode node, Regex regex, Dictionary<string, object> data)
        {
            var matches = regex.Matches(node.InnerHtml);
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;
                if (data[placeholder] != null)
                {
                    node.InnerHtml = node.InnerHtml.Replace($"{{{placeholder}}}", data[placeholder]?.ToString() ?? string.Empty);
                }
                else
                {
                    node.InnerHtml = node.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                }
            }
        }

        private void RemoveOldChildren(HtmlNode currentbody, List<HtmlNode> childs, List<HtmlNode> childGroups)
        {
            foreach (var item in childs)
            {
                currentbody.RemoveChild(item);
            }
            foreach (var item in childGroups)
            {
                currentbody.RemoveChild(item);
            }
        }

        private void ReplaceCTableNode(CreateHtmlVM createHtmlVM, HtmlNode htmlNode, Dictionary<string, Component> dirCom)
        {
            if (htmlNode.Name == "tbody" && htmlNode.Attributes["data-table"] != null)
            {
                var tableName = htmlNode.Attributes["data-table"].Value?.ToString();
                var dollarCurlyBraceRegex = new Regex(@"\{(.+?)\}");
                var curlyBraceRegex = new Regex(@"\#{(.+?)\}");
                var match = dollarCurlyBraceRegex.Match(htmlNode.InnerHtml);
                HtmlNode lastTr = null;
                if (!match.Success)
                {
                    return;
                }
                var currentbody = htmlNode;
                if (lastTr == currentbody)
                {
                    return;
                }
                lastTr = currentbody;
                var valueWithinCurlyBraces = match.Groups[1].Value;
                if (!createHtmlVM.Data.TryGetValue(tableName, out var mainData))
                {
                    return;
                }
                var mainObject = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(JsonConvert.SerializeObject(mainData));
                var childs = currentbody.ChildNodes.Where(x => x.Name != "#text").Where(x => curlyBraceRegex.Match(x.InnerHtml).Length == 0).ToList();
                var childGroups = currentbody.ChildNodes.Where(x => x.Name != "#text").Where(x =>
                {
                    return curlyBraceRegex.Match(x.InnerHtml).Length > 0;
                }).ToList();
                if (childGroups.Count == 2)
                {
                    var groupByLevel1 = currentbody.Attributes["data-group"]?.Value.Split("|").Select(g => g.Trim()).ToArray();
                    if (groupByLevel1.Length == 2)
                    {
                        var groupByLevel21 = groupByLevel1[0];
                        var dataGroupsLevel1 = mainObject.GroupBy(x =>
                        {
                            return x[groupByLevel21];
                        });
                        foreach (var groupLevel1 in dataGroupsLevel1)
                        {
                            var firstLevelItem = groupLevel1.FirstOrDefault();
                            var newGroupLevel1 = childGroups[0].CloneNode(true);
                            ProcessPlaceholders(newGroupLevel1, curlyBraceRegex, firstLevelItem);
                            currentbody.InsertBefore(newGroupLevel1, lastTr.LastChild);
                            var groupByLevel22 = groupByLevel1[1];
                            var dataGroupsLevel2 = groupLevel1.GroupBy(x =>
                            {
                                return x[groupByLevel22];
                            });
                            foreach (var groupLevel2 in dataGroupsLevel2)
                            {
                                var secondLevelItem = groupLevel2.FirstOrDefault();
                                var newGroupLevel2 = childGroups[1].CloneNode(true);
                                ProcessPlaceholders(newGroupLevel2, curlyBraceRegex, secondLevelItem);
                                currentbody.InsertBefore(newGroupLevel2, lastTr.LastChild);
                                foreach (var field in groupLevel2)
                                {
                                    var newRows = childs.Select(x => x.CloneNode(true)).ToList();
                                    foreach (var row in newRows)
                                    {
                                        ProcessPlaceholders2(row, dollarCurlyBraceRegex, field);
                                        currentbody.InsertBefore(row, lastTr.LastChild);
                                    }
                                }
                            }
                        }
                        RemoveOldChildren(currentbody, childs, childGroups);
                    }
                }
                else if (childGroups.Count == 1)
                {
                    var groupBy = currentbody.Attributes["data-group"]?.Value.Split(",").Select(g => g.Trim()).ToArray();
                    var dataGroups = mainObject.GroupBy(x =>
                    {
                        var groupKey = new List<object>();
                        foreach (var key in groupBy)
                        {
                            if (x.ContainsKey(key)) groupKey.Add(x[key]);
                        }
                        return groupKey;
                    });
                    foreach (var group in dataGroups)
                    {
                        var first = group.FirstOrDefault();
                        var newGroupRows = childGroups.Select(x => x.CloneNode(true)).ToList();
                        foreach (var cell in newGroupRows.SelectMany(x => x.ChildNodes).Where(x => x.Name != "#text").ToList())
                        {
                            var cellMatches = dollarCurlyBraceRegex.Matches(cell.InnerText);
                            foreach (Match cellMatch in cellMatches)
                            {
                                if (!cellMatch.Success) continue;
                                var placeholder = cellMatch.Groups[1].Value;
                                try
                                {
                                    var htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(placeholder);
                                    string plainText = htmlDoc.DocumentNode.InnerText;
                                    var currentData = first.GetValueOrNull(plainText);
                                    if (currentData != null)
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                    }
                                    else
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                                    }
                                }
                                catch (Exception e)
                                {
                                }
                            }
                        }
                        foreach (var item in newGroupRows)
                        {
                            item.InnerHtml = FormatString(item.InnerHtml, first);
                            currentbody.InsertBefore(item, lastTr.LastChild);
                        }
                        foreach (var field in group)
                        {
                            var newRows = childs.Select(x => x.CloneNode(true)).ToList();
                            foreach (var cell in newRows.SelectMany(x => x.ChildNodes).Where(x => x.Name != "#text").ToList())
                            {
                                var cellMatches = dollarCurlyBraceRegex.Matches(cell.InnerText);
                                foreach (Match cellMatch in cellMatches)
                                {
                                    if (!cellMatch.Success) continue;
                                    var placeholder = cellMatch.Groups[1].Value;
                                    var placeholderFields = placeholder.Split(".");
                                    if (placeholderFields.Length < 2) continue;

                                    try
                                    {
                                        var htmlDoc = new HtmlDocument();
                                        htmlDoc.LoadHtml(placeholderFields[1]);
                                        string plainText = htmlDoc.DocumentNode.InnerText;
                                        var currentData = field.GetValueOrNull(plainText);
                                        if (currentData != null)
                                        {
                                            cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                        }
                                        else
                                        {
                                            cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                            }
                            foreach (var item in newRows)
                            {
                                item.InnerHtml = FormatString(item.InnerHtml, field);
                                currentbody.InsertBefore(item, lastTr.LastChild);
                            }
                        }
                    }
                    foreach (var item in childs)
                    {
                        currentbody.RemoveChild(item);
                    }
                    foreach (var item in childGroups)
                    {
                        currentbody.RemoveChild(item);
                    }
                }
                else
                {
                    foreach (var field in mainObject)
                    {
                        var newRows = childs.Select(x => x.CloneNode(true)).ToList();
                        foreach (var cell in newRows.SelectMany(x => x.ChildNodes).Where(x => x.Name != "#text").ToList())
                        {
                            var cellMatches = dollarCurlyBraceRegex.Matches(cell.InnerText);
                            foreach (Match cellMatch in cellMatches)
                            {
                                if (!cellMatch.Success) continue;
                                var placeholder = cellMatch.Groups[1].Value;
                                try
                                {
                                    var htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(placeholder);
                                    string plainText = htmlDoc.DocumentNode.InnerText;
                                    var currentData = field.GetValueOrNull(plainText);
                                    if (currentData == null)
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                                    }
                                    else
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                    }
                                }
                                catch (Exception e)
                                {
                                }
                            }
                        }
                        foreach (var item in newRows)
                        {
                            item.InnerHtml = FormatString(item.InnerHtml, field);
                            currentbody.InsertBefore(item, lastTr.LastChild);
                        }
                    }
                }
            }
            try
            {
                foreach (var item in htmlNode.ChildNodes.ToList())
                {
                    ReplaceCTableNode(createHtmlVM, item, dirCom);
                }
            }
            catch (Exception e)
            {
                // Handle exception
            }
        }

        private void ReplaceTableNode(CreateHtmlVM createHtmlVM, HtmlNode htmlNode, Dictionary<string, Component> dirCom)
        {
            if (htmlNode.Name == "tbody" && htmlNode.Attributes["data-table"] != null)
            {
                var dollarCurlyBraceRegex = new Regex(@"\{(.+?)\}");
                var tableName = htmlNode.Attributes["data-table"].Value?.ToString();
                var match = dollarCurlyBraceRegex.Match(htmlNode.InnerHtml);
                HtmlNode lastTr = null;
                if (!match.Success)
                {
                    return;
                }
                var currentbody = htmlNode;
                if (lastTr == currentbody)
                {
                    return;
                }
                var valueWithinCurlyBraces = match.Groups[1].Value;
                if (!createHtmlVM.Data.TryGetValue(tableName, out var mainData))
                {
                    return;
                }
                var mainObject = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(JsonConvert.SerializeObject(mainData));
                var headers = JsonConvert.DeserializeObject<List<Component>>(JsonConvert.SerializeObject(createHtmlVM.Data[tableName + "h"]));
                var childs = currentbody.ChildNodes.Where(x => x.Name != "#text").Where(x => dollarCurlyBraceRegex.Match(x.InnerHtml).Length > 0).ToList();
                foreach (var field in mainObject)
                {
                    var newRows = childs.Select(x => x.CloneNode(true)).ToList();
                    foreach (var cell in newRows.SelectMany(x => x.ChildNodes).Where(x => x.Name != "#text").ToList())
                    {
                        var cellMatches = dollarCurlyBraceRegex.Matches(cell.InnerText);
                        foreach (Match cellMatch in cellMatches)
                        {
                            if (!cellMatch.Success) continue;
                            var placeholder = cellMatch.Groups[1].Value;
                            var curentComponent = headers.FirstOrDefault(x => x.Label == placeholder);
                            if (curentComponent == null)
                            {
                                var currentData = field.GetValueOrNull(placeholder);
                                if (currentData == null)
                                {
                                    cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                                }
                                else
                                {
                                    cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                }
                            }
                            else
                            {
                                try
                                {
                                    var currentData = field.GetValueOrNull(curentComponent.FieldName);
                                    if (currentData == null)
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", string.Empty);
                                    }
                                    else
                                    {
                                        switch (curentComponent.ComponentType)
                                        {
                                            case "Datepicker":
                                                currentData = currentData is null ? "" : Convert.ToDateTime(currentData).ToString("dd/MM/yyyy");
                                                break;
                                            case "Number":
                                                currentData = currentData is null ? "" : Convert.ToDecimal(currentData).ToString(curentComponent.FormatData ?? "N0");
                                                break;
                                            case "Dropdown":
                                                var displayField = curentComponent.FieldName;
                                                var containId = curentComponent.FieldName.EndsWith("Id");
                                                displayField = containId ? displayField.Substring(0, displayField.Length - 2) : displayField + "MasterData";
                                                currentData = FormatString(curentComponent.FormatData, JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(field[displayField])));
                                                break;
                                            default:
                                                break;
                                        }
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new Exception($"Error processing field {curentComponent.FieldName}: {e.Message}", e);
                                }
                            }
                        }
                    }
                    foreach (var item in newRows)
                    {
                        item.InnerHtml = FormatString(item.InnerHtml, field);
                        currentbody.InsertBefore(item, currentbody.LastChild);
                    }
                    foreach (var item in childs)
                    {
                        currentbody.RemoveChild(item);
                    }
                }
            }
            try
            {
                foreach (var item in htmlNode.ChildNodes.ToList())
                {
                    ReplaceTableNode(createHtmlVM, item, dirCom);
                }
            }
            catch (Exception e)
            {
                // Handle exception
            }
        }
    }
}

using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Collections;
using System.Text.RegularExpressions;

namespace CoreAPI.Services
{
    public class PdfService
    {
        public async Task<string> CreateHtml(CreateHtmlVM createHtmlVM, string conn)
        {
            var component = await BgExt.ReadDsAs<Component>($"SELECT * FROM [Component] where Id = '{createHtmlVM.ComId}'", conn);
            var components = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and ComponentGroupId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var gridPolicys = await BgExt.ReadDsAsArr<Component>($"SELECT * FROM [Component] where Label is not null and Label != '' and FeatureId = '{component.FeatureId}' and EntityId is not null and ComponentType not in ('Button','Section','GridView')", conn);
            var dirCom = components.DistinctBy(x => x.Label).ToDictionary(x => x.Label);
            var sql = component.Query;
            if (!sql.IsNullOrWhiteSpace())
            {
                var sqlQuery = FormatString(sql, createHtmlVM.Data);
                var customData = await BgExt.ReadDataSet(sqlQuery, conn);
                int i = 0;
                foreach (var row in customData)
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
                    html = html.Replace($"{{{valueWithinCurlyBraces}}}", data.GetValueOrDefault(plainText)?.ToString());
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
                                currentData = FormatString(curentComponent.FormatData, JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(createHtmlVM.Data.GetValueOrDefault(displayField))));
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
                var mapField = placeholder.Split(".")[1];
                if (data.TryGetValue(mapField, out var value))
                {
                    node.InnerHtml = node.InnerHtml.Replace($"#{{c{placeholder}}}", value?.ToString() ?? string.Empty);
                }
            }
        }

        private void ProcessPlaceholders2(HtmlNode node, Regex regex, Dictionary<string, object> data)
        {
            var matches = regex.Matches(node.InnerHtml);
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;
                var mapField = placeholder.Split(".")[1];
                if (data.TryGetValue(mapField, out var value))
                {
                    node.InnerHtml = node.InnerHtml.Replace($"{{c{placeholder}}}", value?.ToString() ?? string.Empty);
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
            if (htmlNode.Name == "tbody")
            {
                var dollarCurlyBraceRegex = new Regex(@"\{c(.+?)\}");
                var curlyBraceRegex = new Regex(@"\#{c(.+?)\}");
                var match = dollarCurlyBraceRegex.Match(htmlNode.InnerHtml);
                HtmlNode lastTr = null;
                if (!match.Success)
                {
                    return;
                }
                var currentbody = FindClosest(htmlNode, "tbody");
                if (lastTr == currentbody)
                {
                    return;
                }
                lastTr = currentbody;
                var valueWithinCurlyBraces = match.Groups[1].Value;
                string[] fields = valueWithinCurlyBraces.Split(".");
                if (fields.Length < 2)
                {
                    return;
                }
                // Check if there is corresponding data in the view model
                if (!createHtmlVM.Data.TryGetValue("c" + fields[0], out var mainData))
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
                                var placeholderFields = placeholder.Split(".");
                                if (placeholderFields.Length < 2) continue;

                                try
                                {
                                    var htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(placeholderFields[1]);
                                    string plainText = htmlDoc.DocumentNode.InnerText;
                                    var currentData = first[plainText];
                                    cell.InnerHtml = cell.InnerHtml.Replace($"#{{c{placeholder}}}", currentData?.ToString() ?? string.Empty);
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
                                        var currentData = field[plainText];
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{c{placeholder}}}", currentData?.ToString() ?? string.Empty);
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
                                var placeholderFields = placeholder.Split(".");
                                if (placeholderFields.Length < 2) continue;
                                try
                                {
                                    var htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(placeholderFields[1]);
                                    string plainText = htmlDoc.DocumentNode.InnerText;
                                    var currentData = field[plainText];
                                    if (currentData == null)
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{c{placeholder}}}", string.Empty);
                                    }
                                    else
                                    {
                                        cell.InnerHtml = cell.InnerHtml.Replace($"{{c{placeholder}}}", currentData?.ToString() ?? string.Empty);
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
            if (htmlNode.Name == "tr")
            {
                var dollarCurlyBraceRegex = new Regex(@"\{t(.+?)\}");
                var match = dollarCurlyBraceRegex.Match(htmlNode.InnerHtml);
                HtmlNode lastTr = null;
                if (!match.Success)
                {
                    return;
                }
                var currentTr = FindClosest(htmlNode, "tr");
                if (lastTr == currentTr)
                {
                    return;
                }
                lastTr = currentTr;
                var valueWithinCurlyBraces = match.Groups[1].Value;
                string[] fields = valueWithinCurlyBraces.Split(".");
                if (fields.Length < 2)
                {
                    return;
                }
                // Check if there is corresponding data in the view model
                if (!createHtmlVM.Data.TryGetValue("t" + fields[0], out var mainData))
                {
                    return;
                }
                var mainObject = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(JsonConvert.SerializeObject(mainData));
                var headers = JsonConvert.DeserializeObject<List<Component>>(JsonConvert.SerializeObject(createHtmlVM.Data.GetValueOrDefault("t" + fields[0] + "h")));
                foreach (var field in mainObject)
                {
                    var newRow = currentTr.CloneNode(true);

                    foreach (var cell in newRow.ChildNodes.ToList())
                    {
                        var cellMatches = dollarCurlyBraceRegex.Matches(cell.InnerText);
                        foreach (Match cellMatch in cellMatches)
                        {
                            if (!cellMatch.Success) continue;

                            var placeholder = cellMatch.Groups[1].Value;
                            var placeholderFields = placeholder.Split(".");
                            if (placeholderFields.Length < 2) continue;

                            var curentComponent = headers.FirstOrDefault(x => x.Label == placeholderFields[1]);
                            if (curentComponent == null) continue;

                            try
                            {
                                var currentData = field[curentComponent.FieldName];
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
                                            currentData = FormatString(curentComponent.FormatData, JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(field.GetValueOrDefault(displayField))));
                                            break;
                                        default:
                                            break;
                                    }
                                    cell.InnerHtml = cell.InnerHtml.Replace($"{{t{placeholder}}}", currentData?.ToString() ?? string.Empty);
                                }
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Error processing field {curentComponent.FieldName}: {e.Message}", e);
                            }
                        }
                    }

                    var currentTbody = FindClosest(htmlNode, "tbody");
                    newRow.InnerHtml = FormatString(newRow.InnerHtml, field);
                    currentTbody.InsertBefore(newRow, lastTr);
                }
                currentTr.ParentNode.RemoveChild(currentTr);
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

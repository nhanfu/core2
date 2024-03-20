using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class DocumentWrite : Label
    {
        public DocumentWrite(Component ui, HTMLElement ele = null) : base(ui)
        {
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
            Element = ele;
        }

        public override void Render()
        {
            var cellData = Entity.GetPropValue(Meta.FieldName);
            var isBool = cellData != null && cellData.GetType().IsBool();
            string cellText = string.Empty;
            if (Element is null)
            {
                RenderNewEle(cellText);
            }
            if (Meta.Query.HasAnyChar())
            {
                RenderCellText();
            }
            else
            {
                cellText = CalcCellText(new object[] { cellData });
                UpdateEle(cellText);
            }
            if (Meta.IsRealtime)
            {
                Window.SetInterval(RenderCellText, 2000);
            }
        }

        private void UpdateEle(string cellText)
        {
            Element.InnerHTML = cellText;
        }

        private void RenderNewEle(string cellText)
        {
            Html.Take(ParentElement);
            Html.Instance.Div.InnerHTML(cellText);
            Element = Html.Context;
            Html.Instance.End.Render();
        }

        private string CalcCellText(object[] cellData)
        {
            var cellText = Utils.GetHtmlCode(Meta.FormatEntity, cellData);
            if (cellText is null || cellText == "null")
            {
                cellText = "<div><div>";
            }
            return cellText;
        }

        protected Task<object> TryGetData()
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                var isFnPowerQuery = Utils.IsFunction(Meta.Query, out var fn);
                if (isFnPowerQuery)
                {
                    var query = fn.Call(this, Entity, EditForm);
                    tcs.SetResult(query);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return tcs.Task;
        }

        private void RenderCellText()
        {
            if (Meta.Query.IsNullOrEmpty())
            {
                return;
            }
            object[] data = null;
            TryGetData().Done(rsData =>
            {
                var div = Document.CreateElement("div");
                var text = string.Empty;
                if (rsData is object[])
                {
                    data = rsData as object[];
                    text = Utils.GetHtmlCode(Meta.FormatEntity, data);
                    UpdateEle(text);
                }
                else if (rsData is string qr)
                {
                    LoadData(qr).Done(d =>
                    {
                        div.InnerHTML = Meta.FormatEntity;
                        BindingGrid(div, d);
                        text = Utils.GetHtmlCode(div.InnerHTML, d[0]);
                        div.InnerHTML = text;
                        UpdateEle(text);
                    });
                }
                else
                {
                    data = new object[] { rsData };
                    text = Utils.GetHtmlCode(Meta.FormatEntity, data);
                    div.InnerHTML = text;
                    UpdateEle(text);
                }
            });
        }

        private Task<object[][]> LoadData(string qr)
        {
            var tcs = new TaskCompletionSource<object[][]>();
            var isFn = Utils.IsFunction(qr, out var fn);
            var sql = new SqlViewModel
            {
                ComId = Meta.Id,
                Params = isFn ? JSON.Stringify(fn.Call(null, this)) : null,
                MetaConn = MetaConn,
                DataConn = DataConn,
                WrapQuery = false
            };
            Client.Instance.ComQuery(sql).Done(ds =>
            {
                tcs.TrySetResult(ds);
            });
            return tcs.Task;
        }

        protected HTMLElement[] BindingRowData(HTMLElement[] template, object arrItem)
        {
            if (arrItem is null || template.Nothing())
            {
                return null;
            }
            var res = new List<HTMLElement>();
            for (int i = 0; i < template.Length; i++)
            {
                template[i].InnerHTML = Utils.GetHtmlCode(template[i].InnerHTML, new object[] { arrItem });
                res.Add(template[i]);
            }
            return res.ToArray();
        }

        protected HTMLElement[] CloneRow(HTMLElement[] templateRow)
        {
            var rs = templateRow.Select(x => x.CloneNode(true) as HTMLElement).ToArray();
            return rs;
        }

        private void BindingGrid(HTMLElement div, object[] res)
        {
            var dsCount = 0;
            foreach (var child in div.Children)
            {
                EditForm.BindingTemplate(child, this, res, factory: (eleChild, component, parent, entity) =>
                {
                    var com = new Section(MVVM.ElementType.table)
                    {
                        Element = eleChild
                    };
                    if (eleChild.Dataset["grid"] != null)
                    {
                        dsCount++;
                        var ds = res[dsCount];

                        var bodyElemnt = eleChild.Children;
                        var arr = ds as object[];
                        if (arr.Nothing())
                        {
                            arr = new object[] { new object() };
                        }
                        var formattedRows = arr.Select(arrItem =>
                        {
                            var cloned = CloneRow(bodyElemnt.ToArray());
                            return BindingRowData(cloned, arrItem);
                        }).SelectMany(x => x).ToArray();
                        foreach (var ele in eleChild.Children)
                        {
                            eleChild.RemoveChild(ele);
                        }
                        formattedRows.ForEach(x => eleChild.AppendChild(x));
                    }
                    return com;
                });
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            PrepareUpdateView(force, dirty);
            Render();
        }

        public override string GetValueTextAct()
        {
            return Element.TextContent;
        }
    }
}

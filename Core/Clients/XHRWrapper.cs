using Bridge.Html5;
using Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Clients
{
    public class XHRWrapper
    {
        public bool NoQueue { get; set; }
        public bool Retry { get; set; }
        public bool ShowError { get; set; }
        public bool AllowAnonymous { get; set; }
        public bool AddTenant { get; set; }
        public bool AllowNestedObject { get; set; }
        public HttpMethod Method { get; set; } = HttpMethod.GET;
        public string Url { get; set; }
        public string NameSpace { get; internal set; }
        public string Prefix { get; set; }
        public string EntityName { get; set; }
        public string FinalUrl { get; internal set; }
        public string ResponseMimeType { get; set; }
        public object Value { get; set; }
        public bool IsRaw { get; set; }
        
        public string JsonData
        {
            get
            {
                if (Value == null)
                {
                    return null;
                }
                if (IsRaw && Value is string val)
                {
                    return val;
                }
                string res = null;

                try
                {
                    res = JsonConvert.SerializeObject(Value);
                }
                catch (Exception)
                {
                    try
                    {
                        res = JsonConvert.SerializeObject(Value);
                    }
                    catch (Exception)
                    {
                        UnboxValue(Value);
                        res = res ?? JSON.Stringify(Value);
                    }
                }
                return res;
            }
        }

        private void UnboxValue(object val)
        {
            /*@
            for (var i in val) {
                if (val[i] === null) continue;
                if (val[i].v !== undefined) {
                    val[i] = val[i].v;
                } else if (val[i]._items !== undefined) {
                    val[i] = val[i]._items;
                }
                if (val[i] instanceof Array) {
                    for (var j in val[i]) this.UnboxValue(val[i][j]);
                }
            }
            */
        }

        public FormData FormData { get; set; }
        public File File { get; set; }
        public Action<object> ProgressHandler { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Func<object, object> CustomParser { get; set; }
        public Action<XMLHttpRequest> ErrorHandler { get; set; }
    }
}

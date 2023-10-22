using Bridge.Html5;
using Core.Enums;
using Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Clients
{
    public class XHRWrapper
    {
        public bool AllowNested { get; set; }
        public bool NoQueue { get; set; }
        public bool Retry { get; set; }
        public bool ShowError { get; set; }
        public bool AllowAnonymous { get; set; }
        public bool AddTenant { get; set; }
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
                
                return JsonConvert.SerializeObject(AllowNested ? Value : UnboxValue(Value));
            }
        }

        private object UnboxValue(object val)
        {
            if (val == null) return null;
            var res = new object();
            foreach (var prop in val.GetType().GetProperties())
            {
                if (prop.PropertyType.IsSimple()) 
                {
                    res[prop.Name] = val[prop.Name];
                }
            }
            return res;
        }

        public FormData FormData { get; set; }
        public File File { get; set; }
        public Action<object> ProgressHandler { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Func<object, object> CustomParser { get; set; }
        public Action<XMLHttpRequest> ErrorHandler { get; set; }
    }
}

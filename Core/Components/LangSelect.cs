using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class LangSelect : EditableComponent
    {
        public const string LangProp = "langprop";
        public const string LangKey = "langkey";
        public const string LangParam = "Para";
        public const string LangCode = "langcode";
        private const string Active = "Active";
        private static string culture;
        public static Dictionary<string, Dictionary<string, string>> _dictionaries = new Dictionary<string, Dictionary<string, string>>();

        public static string Culture
        {
            get
            {
                if (culture != null)
                {
                    return culture?.Replace("\"", "");
                }
                var res = LocalStorage.GetItem<string>(nameof(Culture));
                if (res != null)
                {
                    culture = res?.Replace("\"", "");
                }
                return res?.Replace("\"", "");
            }
            set
            {
                if (culture == value)
                {
                    return;
                }
                culture = value?.Replace("\"","");
                LocalStorage.SetItem(nameof(Culture), value?.Replace("\"", ""));
            }
        }

        public LangSelect(Component com, HTMLElement ele) : base(com)
        {
            ParentElement = ele;
        }

        public override void Render()
        {
            if (GuiInfo != null && !GuiInfo.FormatData.IsNullOrEmpty() && Utils.IsFunction(GuiInfo.FormatData, out var fn))
            {
                Html.Take(ParentElement);
                fn.Call(null, this);
                Element = ParentElement.FirstElementChild;
            }
            if (Element == null)
            {
                Html.Take(ParentElement).Ul.ClassName("lang-select")
                    .Li.DataAttr(LangCode, "vi").Attr(Active, true.ToString().ToLower()).Img.Src("./icons/vn.png").EndOf(MVVM.ElementType.li)
                    .Li.DataAttr(LangCode, "en").Img.Src("./icons/eg.png").EndOf(MVVM.ElementType.li);
                Element = Html.Context;
            }
            Travel(Element).Where(x => x is HTMLElement).Cast<HTMLElement>().ForEach(x =>
            {
                var code = x.Dataset[LangCode];
                if (code.IsNullOrEmpty())
                {
                    return;
                }
                x.AddEventListener(EventType.Click, (e) =>
                {
                    if (x.GetAttribute(Active) == true.ToString().ToLower())
                    {
                        return;
                    }
                    Element.QuerySelectorAll("[Active=true]").Cast<HTMLElement>().ForEach(li => li.RemoveAttribute(Active));
                    x.SetAttribute(Active, true.ToString().ToLower());
                    SetCultureAndTranslate(code);
                });
                if (Culture is null && x.GetAttribute(Active) == true.ToString().ToLower())
                {
                    SetCultureAndTranslate(code);
                }
            });
            Html.Take(ParentElement);
        }

        private static void SetCultureAndTranslate(string code)
        {
            Culture = code;
            Task.Run(Translate);
        }

        public static string Get(string key)
        {
            if (key.IsNullOrEmpty())
            {
                return string.Empty;
            }
            if (Culture is null)
            {
                return key;
            }
            var dictionary = _dictionaries.GetValueOrDefault(Culture);
            if (dictionary == null)
            {
                dictionary = LocalStorage.GetItem<Dictionary<string, string>>(Culture);
                if (_dictionaries.ContainsKey(Culture))
                {
                    _dictionaries.Remove(Culture);
                }
                _dictionaries.TryAdd(Culture, dictionary);
            }
            return dictionary != null && dictionary.ContainsKey(key) ? dictionary[key] : key;
        }

        public static async Task Translate()
        {
            var dictionaryItems = await new Client(nameof(Dictionary))
                .GetRawList<Dictionary>($"/?t={Client.Tenant}&$filter=LangCode eq '{Culture}'", addTenant: true, annonymous: true);
            var map = dictionaryItems.ToDictionary(x => x.Key, x => x.Value);
            if (_dictionaries.ContainsKey(Culture))
            {
                _dictionaries.Remove(Culture);
                _dictionaries.Add(Culture, map);
            }
            LocalStorage.SetItem(Culture, map);
            // find all element that should translate
            Travel(Document.Instance).SelectForeach(x =>
            {
                var props = x[LangProp]?.ToString();
                if (props is null)
                {
                    return;
                }
                props.Split(",").ForEach(propName =>
                {
                    var template = x[LangKey + propName]?.ToString();
                    var parameters = x[LangParam + propName] as object[];

                    var translated = map.ContainsKey(template) ? map[template] : template;
                    translated = parameters.HasElement() ? string.Format(translated, parameters) : translated;
                    if (x is Text text && propName == nameof(Text.TextContent))
                    {
                        text.TextContent = translated;
                        return;
                    }
                    var ele = x as HTMLElement;
                    if (propName == nameof(HTMLElement.ClassName))
                    {
                        ele.ClassName = ele.ClassName.Replace(template, translated);
                    }
                    else
                    {
                        ele.SetAttribute(propName, translated);
                    }
                });
            });
        }

        public static IEnumerable<Node> Travel(Node node)
        {
            foreach (var element in node.ChildNodes)
            {
                yield return element;
                foreach (var inner in Travel(element))
                {
                    yield return inner;
                }
            }
        }
    }
}

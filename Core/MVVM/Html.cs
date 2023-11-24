using Bridge.Html5;
using Core.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.MVVM
{
    public class Html
    {
        private static Html _instance;
        public static HTMLElement Context { get; set; }

        public Html(string selector)
        {
            Context = Document.QuerySelector(selector) as HTMLElement;
        }

        public Html()
        {

        }

        public Html(HTMLElement ele)
        {
            Context = ele;
        }

        public static Html Take(string selector)
        {
            Context = Document.QuerySelector(selector) as HTMLElement;
            return Instance;
        }

        public static Html Take(HTMLElement ele)
        {
            Context = ele;
            return Instance;
        }

        public HTMLElement GetContext()
        {
            return Context;
        }

        public static Html Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Html();
                }

                return _instance;
            }
        }

        public Html Div
        {
            get
            {
                return Add(ElementType.div);
            }
        }

        public Html Link
        {
            get
            {
                return Add(ElementType.link);
            }
        }

        public Html Script
        {
            get
            {
                return Add(ElementType.script);
            }
        }

        public Html Header
        {
            get
            {
                return Add(ElementType.header);
            }
        }

        public Html Section
        {
            get
            {
                return Add(ElementType.section);
            }
        }

        public Html Canvas
        {
            get
            {
                return Add(ElementType.canvas);
            }
        }

        public Html Video
        {
            get
            {
                return Add(ElementType.video);
            }
        }

        public Html Audio
        {
            get
            {
                return Add(ElementType.audio);
            }
        }

        public Html H1
        {
            get
            {
                return Add(ElementType.h1);
            }
        }

        public Html H2
        {
            get
            {
                return Add(ElementType.h2);
            }
        }

        public Html H3
        {
            get
            {
                return Add(ElementType.h3);
            }
        }

        public Html H4
        {
            get
            {
                return Add(ElementType.h4);
            }
        }

        public Html H5
        {
            get
            {
                return Add(ElementType.h5);
            }
        }

        public Html H6
        {
            get
            {
                return Add(ElementType.h6);
            }
        }

        public Html Nav
        {
            get
            {
                return Add(ElementType.nav);
            }
        }

        public Html Input
        {
            get
            {
                return Add(ElementType.input);
            }
        }

        public Html Select
        {
            get
            {
                return Add(ElementType.select);
            }
        }

        public Html Option
        {
            get
            {
                return Add(ElementType.option);
            }
        }

        public Html Span
        {
            get
            {
                return Add(ElementType.span);
            }
        }

        public Html Small
        {
            get
            {
                return Add(ElementType.small);
            }
        }

        public Html I
        {
            get
            {
                return Add(ElementType.i);
            }
        }

        public Html Img
        {
            get
            {
                return Add(ElementType.img);
            }
        }

        public Html Button
        {
            get
            {
                return Add(ElementType.button);
            }
        }

        public Html Table
        {
            get
            {
                return Add(ElementType.table);
            }
        }

        public Html Thead
        {
            get
            {
                return Add(ElementType.thead);
            }
        }

        public Html Th
        {
            get
            {
                return Add(ElementType.th);
            }
        }

        public Html TBody
        {
            get
            {
                return Add(ElementType.tbody);
            }
        }

        public Html TFooter
        {
            get
            {
                return Add(ElementType.tfoot);
            }
        }

        public Html TRow
        {
            get
            {
                return Add(ElementType.tr);
            }
        }

        public Html TData
        {
            get
            {
                return Add(ElementType.td);
            }
        }

        public Html P
        {
            get
            {
                return Add(ElementType.p);
            }
        }

        public Html TextArea
        {
            get
            {
                return Add(ElementType.textarea);
            }
        }

        public Html Br
        {
            get
            {
                var br = new HTMLBRElement();
                Context.AppendChild(br);
                return this;
            }
        }

        public Html Hr
        {
            get
            {
                var hr = new HTMLHRElement();
                Context.AppendChild(hr);
                return this;
            }
        }

        public Html Ul
        {
            get
            {
                return Add(ElementType.ul);
            }
        }

        public Html Li
        {
            get
            {
                return Add(ElementType.li);
            }
        }

        public Html Aside
        {
            get
            {
                return Add(ElementType.aside);
            }
        }

        public Html A
        {
            get
            {
                return Add(ElementType.a);
            }
        }

        public Html Form
        {
            get
            {
                return Add(ElementType.form);
            }
        }

        public Html Label
        {
            get
            {
                return Add(ElementType.label);
            }
        }

        public Html End
        {
            get
            {
                Context = Context.ParentElement;
                return this;
            }
        }

        public Html EndOf(ElementType type)
        {
            return EndOf(type.ToString());
        }

        public Html EndOf(string selector)
        {
            var result = Context;
            while (result != null)
            {
                if (result.QuerySelector(selector) != null)
                {
                    break;
                }
                else
                {
                    result = result.ParentElement;
                }
            }

            Context = result ?? throw new InvalidOperationException("Cannot find the element of selector " + selector);
            return this;
        }

        public Html Closest(ElementType type)
        {
            var func = Context["closest"] as Func<string, HTMLElement>;
            Context = (HTMLElement)func.Call(Context, type.ToString());
            return this;
        }

        public void Render(string html)
        {
            Context.InnerHTML = html;
        }

        public void Render()
        {
            // Method intentionally left empty.
        }

        public Html Add(ElementType type)
        {
            var ele = Document.CreateElement(type.ToString());
            Context.AppendChild(ele);
            Context = ele;
            return this;
        }

        public Html Id(string id)
        {
            Context.Id = id;
            return this;
        }

        public Html Style(string style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return this;
            }
            Context.Style.CssText += style;
            return this;
        }

        public Html Style(object style)
        {
            foreach (var key in GetOwnPropertyNames(style))
            {
                Context["style"][key] = style[key];
            }
            return this;
        }

        public Html Clear()
        {
            if (Context != null)
            {
                Context.TextContent = string.Empty;
                Context.InnerHTML = string.Empty;
            }
            return this;
        }

        public Html If(bool condition, Action doif, Action doelse = null)
        {
            if (condition)
            {
                doif();
            }
            else
            {
                doelse();
            }

            return this;
        }

        public Html Checkbox(bool? value)
        {
            Add(ElementType.input);
            var checkbox = Context as HTMLInputElement;
            checkbox.SetAttribute("type", "checkbox");
            checkbox.Checked = value ?? false;
            Event(EventType.Change, (e) =>
            {
                value = (e.Target as HTMLInputElement).Checked;
            });
            Event(EventType.Click, (e) =>
            {
                value = (e.Target as HTMLInputElement).Checked;
            });
            return this;
        }

        public Html Value(string val)
        {
            var input = Context;
            input["value"] = val;
            return this;
        }

        public Html Value<T>(Observable<T> val)
        {
            var input = Context;
            if (input != null)
            {
                input["value"] = val.Data?.ToString();
                Event(EventType.Input, (e) =>
                {
                    SetObservableValue(val, input["value"]?.ToString());
                });
                val.Changed += (arg) =>
                {
                    input["value"] = arg.NewData != null ? arg.NewData.ToString() : string.Empty;
                };
            }
            return this;
        }

        private static void SetObservableValue<T>(Observable<T> val, string value)
        {
            val.Data = string.IsNullOrEmpty(value) ? default(T) : (T)Convert.ChangeType(value, typeof(T));
        }

        public Html Attr(string attr, string val)
        {
            Context.SetAttribute(attr, val);
            return this;
        }

        public Html Attr(string attr, int val)
        {
            Context.SetAttribute(attr, val.ToString());
            return this;
        }

        public Html DataAttr(string attr, string val)
        {
            Context.SetAttribute("data-" + attr, val);
            return this;
        }

        public Html DataAttr(string attr, object obj)
        {
            Context.SetAttribute("data-" + attr, obj.ToString());
            return this;
        }

        public Html Type(string val)
        {
            Context.SetAttribute("type", val);
            return this;
        }

        public Html Type(InputType type)
        {
            Context.SetAttribute("type", type.ToString());
            return this;
        }

        public Html Href(string val)
        {
            Context.SetAttribute("href", val);
            return this;
        }

        public Html Src(string val)
        {
            Context.SetAttribute("src", val);
            return this;
        }

        /// <summary>
        /// Append text node, auto enclosing
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public Html Text(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return this;
            }
            var text = new Text(val);
            Context.AppendChild(text);
            return this;
        }

        public Html InnerHTML(string val)
        {
            Context.InnerHTML = val;
            return this;
        }

        public Html Text<T>(Observable<T> val)
        {
            var text = new Text(val.Data.ToString());
            val.Changed += (arg) =>
             {
                 text.TextContent = arg.NewData?.ToString();
             };
            Context.AppendChild(text);
            return this;
        }

        public Html Number(Observable<int> val)
        {
            var text = new Text(string.Format("{0:n0}", val.Data));
            val.Changed += (arg) =>
            {
                text.TextContent = string.Format("{0:n0}", arg.NewData);
            };
            Context.AppendChild(text);
            return this;
        }

        public Html Event(EventType type, Action action)
        {
            Context.AddEventListener(type, action);
            return this;
        }

        public Html Event(EventType type, Action<Event> action)
        {
            Context.AddEventListener(type, action);
            return this;
        }

        public Html Event<T>(EventType type, Action<Event, T> action, T model)
        {
            Context.AddEventListener(type, (e) =>
            {
                action(e, model);
            });
            return this;
        }

        public Html AsyncEvent<T>(EventType type, Func<T, Task> action, T model)
        {
            Context.AddEventListener(type, (Event e) =>
            {
                Client.ExecTaskNoResult(action(model));
            });
            return this;
        }

        public Html AsyncEvent(EventType type, Func<Event, Task> action)
        {
            Context.AddEventListener(type, (Event e) =>
            {
                Client.ExecTaskNoResult(action(e));
            });
            return this;
        }

        public Html Event<T>(EventType type, Action<T> action, T model)
        {
            Context.AddEventListener(type, (e) =>
            {
                action(model);
            });
            return this;
        }

        public Html Event<T>(EventType type, Action<T, Event> action, T model)
        {
            Context.AddEventListener(type, (e) =>
            {
                action(model, e);
            });
            return this;
        }

        public Html Trigger(EventType type)
        {
            var e = new Event(type.ToString(), new EventInit());
            Context.DispatchEvent(e);
            return this;
        }

        public void ClearContextContent()
        {
            Context.InnerHTML = string.Empty;
        }

        public Html ForEach<T>(IEnumerable<T> list, Action<T, int> renderer)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            var element = Context;

            var length = list.Count();
            var index = -1;
            var enumerator = list.GetEnumerator();

            while (++index < length)
            {
                Context = element;
                enumerator.MoveNext();
                renderer.Call(element, enumerator.Current, index);
            }
            Context = element;
            return this;
        }

        public Html ForEach<T>(IEnumerable<T> list, Action<T> renderer)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            var element = Context;

            var length = list.Count();
            var index = -1;
            var enumerator = list.GetEnumerator();

            while (++index < length)
            {
                Context = element;
                enumerator.MoveNext();
                renderer.Call(element, enumerator.Current, index);
            }
            Context = element;
            return this;
        }

        public Html ForEach<T>(ObservableList<T> observableArray, Action<T, int> renderer)
        {
            if (observableArray == null)
            {
                throw new ArgumentNullException(nameof(observableArray));
            }

            var element = Context;

            var list = observableArray.Data;
            var length = list.Count;
            var index = -1;

            while (++index < length)
            {
                Context = element;
                renderer.Invoke(list[index], index);
            }
            observableArray.ListChanged += (ObservableListArgs<T> e) =>
            {
                e.Element = element;
                e.Renderer = renderer;
                e.Action = e.Action ?? ObservableAction.Render;
                Update(e, observableArray);
            };
            return this;
        }

        private void Update<T>(ObservableListArgs<T> arg, ObservableList<T> observableArray)
        {
            Context = arg.Element;
            int numOfElement;
            switch (arg.Action)
            {
                case ObservableAction.Add:
                    observableArray.Data.Insert(arg.Index, arg.Item);
                    if (arg.Index == arg.ListData.Count - 1)
                    {
                        arg.Renderer.Call(arg.Element, arg.Item, arg.Index);
                        return;
                    }
                    var div = new HTMLDivElement();
                    Context = div;
                    arg.Renderer.Call(div, arg.Item, arg.Index);
                    AppendChildList(arg.Element, div, arg.Index);
                    break;
                case ObservableAction.Remove:
                    observableArray.Data.Remove(arg.Item);
                    numOfElement = arg.Element.Children.Length / (arg.ListData.Count + 1);
                    RemoveChildList(arg.Element, arg.Index, numOfElement);
                    break;
                case ObservableAction.Move:
                    numOfElement = arg.Element.Children.Length / arg.ListData.Count;
                    var newIndex = arg.Index;
                    var oldIndex = arg.ListData.IndexOf(arg.Item);
                    if (newIndex == oldIndex)
                    {
                        return;
                    }

                    SwapElement(observableArray.Data, newIndex, oldIndex);
                    var firstOldElementIndex = oldIndex * numOfElement;
                    var nodeToInsert = oldIndex < newIndex ? arg.Element.Children[(newIndex + 1) * numOfElement] : arg.Element.Children[newIndex * numOfElement];
                    for (var j = 0; j < numOfElement; j++)
                    {
                        arg.Element.InsertBefore(arg.Element.Children[firstOldElementIndex], nodeToInsert);
                        if (oldIndex > newIndex)
                        {
                            firstOldElementIndex++;
                        }
                    }
                    break;
                case ObservableAction.Render:
                    ClearContextContent();
                    var length = arg.ListData.Count;
                    var i = -1;
                    while (++i < length)
                    {
                        Context = arg.Element;
                        arg.Renderer.Call(arg.Element, arg.ListData[i], i);
                    }
                    break;
            }
        }

        private static void SwapElement<T>(List<T> observableArray, int newIndex, int oldIndex)
        {
            var tmpData = observableArray[newIndex];
            observableArray[newIndex] = observableArray[oldIndex];
            observableArray[oldIndex] = tmpData;
        }

        private void AppendChildList(Element parent, Element tmpNode, int index)
        {
            index *= tmpNode.Children.Length;
            Element previousNode = parent.Children[index];
            while (tmpNode.Children.Length > 0)
            {
                parent.InsertBefore(tmpNode.Children[0], previousNode);
            }
        }

        private void RemoveChildList(Element parent, int index, int numOfElement)
        {
            var startIndex = index * numOfElement;
            for (var i = 0; i < numOfElement && parent.Children.Length > 0; i++)
            {
                parent.RemoveChild(parent.Children[startIndex]);
            }
        }

        public Html Dropdown<T>(List<T> list, T selectedItem, string displayField = null, string valueField = null)
        {
            if (Context.NodeName.ToLowerCase() != "select")
            {
                Select.Render();
            }
            var select = Context as HTMLSelectElement;
            list.ForEach((T model) =>
            {
                var text = displayField == null ? model.ToString() : model[displayField]?.ToString();
                var value = valueField == null ? model.ToString() : model[valueField]?.ToString();
                Option.Text(text).Value(value).End.Render();
            });
            select.SelectedIndex = GetSelectedIndex(list, selectedItem, valueField);
            return this;
        }

        public Html Dropdown<T>(ObservableList<T> list, Observable<T> selectedItem, string displayField, string valueField)
        {
            if (Context.NodeName.ToLowerCase() != "select")
            {
                Select.Render();
            }
            var select = Context as HTMLSelectElement;
            ForEach(list, (T model, int index) =>
            {
                var text = model[displayField]?.ToString();
                var value = model[valueField]?.ToString();
                Option.Text(text).Value(value).End.Render();
            });
            select.SelectedIndex = GetSelectedIndex(list.Data.ToList(), selectedItem.Data, valueField);
            list.ListChanged += (realList) =>
            {
                select.SelectedIndex = GetSelectedIndex(realList.ListData.ToList(), selectedItem.Data, valueField);
            };
            Event(EventType.Change, () =>
            {
                var selectedObj = list.Data[select.SelectedIndex];
                selectedItem.Data = selectedObj;
            });
            selectedItem.Changed += (val) =>
            {
                select.SelectedIndex = GetSelectedIndex(list.Data.ToList(), val.NewData, valueField);
            };

            return this;
        }

        private int GetSelectedIndex<T>(List<T> list, T item, string valueField)
        {
            if (item == null)
            {
                return -1;
            }

            var arr = list.ToArray();
            var index = Array.IndexOf(arr, item);
            if (valueField != "")
            {
                var selectedItem = Array.Find(arr, x =>
                {
                    return x[valueField] == item[valueField];
                });
                index = Array.IndexOf(arr, selectedItem);
            }
            return index;
        }

        public Html Visibility(bool visible)
        {
            var ele = Context as HTMLElement;
            ele.Style.Visibility = visible ? "" : Bridge.Html5.Visibility.Hidden.ToString();
            return this;
        }

        public Html Visibility(Observable<bool> visible)
        {
            var ele = Context as HTMLElement;
            ele.Style.Visibility = visible.Data ? "" : Bridge.Html5.Visibility.Hidden.ToString();
            visible.Changed += (arg) =>
            {
                ele.Style.Visibility = arg.NewData ? "" : Bridge.Html5.Visibility.Hidden.ToString();
            };
            return this;
        }

        public Html Display(bool shouldShow)
        {
            var ele = Context as HTMLElement;
            ele.Style.Display = shouldShow ? string.Empty : Bridge.Html5.Display.None.ToString();
            return this;
        }

        public Html Display(Observable<bool> shouldShow)
        {
            var ele = Context as HTMLElement;
            ele.Style.Display = shouldShow.Data ? string.Empty : Bridge.Html5.Display.None.ToString();
            shouldShow.Changed += (arg) =>
            {
                ele.Style.Display = arg.NewData ? string.Empty : Bridge.Html5.Display.None.ToString();
            };
            return this;
        }
    }
}

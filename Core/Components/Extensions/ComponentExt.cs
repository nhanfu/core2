using Bridge.Html5;
using Core.Clients;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components.Extensions
{
    public static class ComponentExt
    {
        private const string Auto = "auto";
        private const int StepPx = 10;

        public static Dictionary<string, Feature> FeatureMap { get; internal set; } = new Dictionary<string, Feature>();

        /// <summary>
        /// This method is used to dispatch UI event to event handler, not from data change
        /// </summary>
        /// <param name="com"></param>
        /// <param name="events"></param>
        /// <param name="eventType"></param>
        /// <param name="parameters"></param>
        public static async Task DispatchEventToHandlerAsync(this EditableComponent com, string events, EventType eventType, params object[] parameters)
        {
            if (events.IsNullOrEmpty())
            {
                return;
            }
            var eventTypeName = eventType.ToString();
            await InvokeEventAsync(com, events, eventTypeName, parameters);
        }

        private static async Task InvokeEventAsync(EditableComponent com, string events, string eventTypeName, params object[] parameters)
        {
            object eventObj;
            try
            {
                eventObj = JsonConvert.DeserializeObject<object>(events);
            }
            catch
            {
                return;
            }
            var form = com.EditForm;
            if (form is null)
            {
                return;
            }
            var eventName = eventObj[eventTypeName]?.ToString();
            var isFn = Utils.IsFunction(eventName, out var func);
            if (isFn)
            {
                func.Call(form, form, com);
                return;
            }
            if (eventName.IsNullOrEmpty())
            {
                return;
            }

            var method = form[eventName];
            if (method is null)
            {
                form = form.FindComponentEvent(eventName);
                if (form is null)
                {
                    return;
                }
                method = form[eventName];
            }
            if (method is null)
            {
                return;
            }

            using (Task task = null)
            {
                /*@
                var task = method.apply(form, parameters);
                if (task == null || task.isCompleted == null) {
                    $tcs.setResult(null);
                    return;
                }
                */
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw ex;
                }
            }
        }

        public static Task DispatchCustomEventAsync(this EditableComponent com, string events, CustomEventType eventType, params object[] parameters)
        {
            if (events.IsNullOrEmpty())
            {
                return Task.FromResult(true);
            }

            var eventTypeName = eventType.ToString();
            return InvokeEventAsync(com, events, eventTypeName, parameters);
        }

        public static Component MapToCom(this object raw)
        {
            var com = raw.CastProp<Component>();
            com.FieldText = raw["FieldText"] as string;
            return com;
        }

        public static PatchVM MapToPatch(this object com, string table)
        {
            var patch = new PatchVM
            {
                Table = table
            };
            com.ForEachProp((prop, val) => {
                patch.Changes.Add(new PatchDetail {
                    Field = prop, Value = val?.ToString()
                });
            });
            return patch;
        }

        public static string MapToFilterOperator(this Component com, string searchTerm)
        {
            if (searchTerm.IsNullOrWhiteSpace() || !com.HasFilter || com.FieldName.IsNullOrEmpty())
            {
                return string.Empty;
            }

            searchTerm = searchTerm.Trim();
            var fieldName = com.ComponentType == nameof(SearchEntry) ? com.FieldText : com.FieldName;
            if (fieldName.IsNullOrWhiteSpace()) return string.Empty;
            if (com.ComponentType == "Datepicker")
            {
                var parsedDate = DateTimeOffset.TryParse(searchTerm, out var date);
                if (parsedDate)
                {
                    var dateStr = date.ToString("yyyy/MM/dd");
                    return $"cast(ds.{fieldName} as date) = cast('{dateStr}' as date)";
                }

                return string.Empty;
            }
            else if (com.ComponentType == "Checkbox")
            {
                var parseBool = bool.TryParse(searchTerm, out bool val);
                if (!parseBool)
                {
                    return string.Empty;
                }

                return $"ds.{fieldName} = {val}";
            }
            else if (com.ComponentType == "Number")
            {
                var parsedNumber = decimal.TryParse(searchTerm, out var searchNumber);
                if (!parsedNumber)
                {
                    return string.Empty;
                }

                return $"ds.{fieldName} = {searchNumber}";
            }
            return com.FilterTemplate.HasAnyChar() ? string.Format(com.FilterTemplate, searchTerm) : $"charindex(N'{searchTerm}', ds.{fieldName}) >= 1";
        }

        public static TabEditor OpenTab(this EditableComponent com, string id, Func<TabEditor> factory)
        {
            if (TabEditor.FindTab(id) is TabEditor tab)
            {
                tab.Focus();
                return tab;
            }
            tab = factory.Invoke();
            OpenTabOrPopup(com, tab);
            return tab;
        }

        public static async Task<TabEditor> OpenTab(this EditableComponent com,
            string id, string featureName, Func<TabEditor> factory, bool popup = false, bool anonymous = false)
        {
            if (!popup && TabEditor.FindTab(id) is TabEditor exists)
            {
                exists.Focus();
                return exists;
            }
            var feature = await LoadFeature(Client.ConnKey, featureName);
            var tab = factory.Invoke();
            tab.Popup = popup;
            tab.Name = featureName;
            tab.Id = id;
            tab.Feature = feature;
            OpenTabOrPopup(com, tab);
            return tab;
        }

        private static void OpenTabOrPopup(EditableComponent com, TabEditor tab)
        {
            var parentTab = com is EditForm editForm ? editForm : com.EditForm ?? com.FindClosest<EditForm>();
            if (tab.Popup)
            {
                com.AddChild(tab);
            }
            else
            {
                tab.Render();
            }
            tab.ParentForm = parentTab;
            tab.OpenFrom = parentTab?.FilterChildren<ListViewItem>(x => x.Entity == tab.Entity)?.FirstOrDefault();
        }

        public static async Task<TabEditor> OpenPopup(this EditableComponent com, string featureName, Func<TabEditor> factory, bool anonymous = false, bool child = false)
        {
            return await com.OpenTab(com.GetHashCode().ToString(), featureName, factory, true, anonymous);
        }

        public static Task<EditForm> InitFeatureByName(string connKey, string hash, bool portal = true)
        {
            var tcs = new TaskCompletionSource<EditForm>();
            var featureName = hash.Replace("-", " ").Replace("#", string.Empty);
            LoadFeature(connKey, featureName).Done(feature =>
            {
                if (feature is null)
                {
                    return;
                }

                var type = Type.GetType(feature.ViewClass);
                var instance = type is null ? new EditForm(string.Empty) : Activator.CreateInstance(type) as EditForm;
                instance.Feature = feature;
                instance.Id = feature.Name;
                EditForm.Portal = portal;
                instance.Render();
                tcs.SetResult(instance);
            });
            return tcs.Task;
        }

        public static Task<Feature> LoadFeature(string connKey, string name, string id = null)
        {
            var tcs = new TaskCompletionSource<Feature>();
#if RELEASE
            if (FeatureMap.ContainsKey(featureName))
            {
                tcs.SetResult(FeatureMap[featureName]);
                return tcs.Task;
            }
#endif
            var featureTask = Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
            {
                Value = JSON.Stringify(new SqlViewModel
                {
                    ComId = "Feature",
                    Action = "GetFeature",
                    ConnKey = Client.ConnKey,
                    Params = JSON.Stringify(new { Name = name, Id = id })
                }),
                Url = Utils.UserSvc,
                IsRawString = true,
                Method = HttpMethod.POST
            });
            Client.ExecTask(featureTask, ds =>
            {
                if (ds.Nothing() || ds[0].Nothing())
                {
                    tcs.TrySetResult(null);
                }
                var feature = ds[0][0].CastProp<Feature>();
                var policies = ds[1].Select(x => x.CastProp<FeaturePolicy>()).ToList();
                var groups = ds[2].Select(x => x.CastProp<ComponentGroup>()).ToList();
                var components = ds[3].Select(x => x.MapToCom()).ToList();
                if (policies.Nothing() || groups.Nothing() || components.Nothing())
                {
                    tcs.TrySetResult(null);
                    return;
                }
                var groupMap = groups.DistinctBy(x => x.Id).ToDictionary(x => x.Id);
                components.ForEach(com =>
                {
                    var g = groupMap.GetValueOrDefault(com.ComponentGroupId);
                    g.Component.Add(com);
                });
                feature.ComponentGroup = groups;
                feature.FeaturePolicy = policies;
                tcs.TrySetResult(feature);
            });

            return tcs.Task;
        }

        public static async Task<List<FeaturePolicy>> LoadRecordPolicy(string[] ids, string entity)
        {
            if (ids.Nothing() || ids.All(x => x == null))
            {
                return new List<FeaturePolicy>();
            }
            return await new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and EntityId eq '{entity}' and RecordId in ({ids.Select(x => $"'{x}'").Combine(",")})");
        }

        public static EditableComponent FirstOrDefault(this EditableComponent component, Func<EditableComponent, bool> predicate, Func<EditableComponent, bool> ignorePredicate = null)
        {
            if (component is null || component.Children.Nothing())
            {
                return null;
            }

            foreach (var child in component.Children)
            {
                if (ignorePredicate != null && ignorePredicate(child))
                {
                    continue;
                }

                if (predicate(child))
                {
                    return child;
                }
            }
            foreach (var child in component.Children)
            {
                if (child.Children.HasElement())
                {
                    var res = child.FirstOrDefault(predicate, ignorePredicate);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<T> FindActiveComponent<T>(this EditableComponent component, Func<T, bool> predicate = null) where T : EditableComponent
        {
            var result = new HashSet<T>();
            var type = typeof(T);
            if (component is null || component.Children is null || component.Children.Nothing())
            {
                return Enumerable.Empty<T>();
            }

            foreach (var child in component.Children)
            {
                if (type.IsAssignableFrom(child.GetType())
                    && child.ParentElement != null
                    && !child.ParentElement.Hidden()
                    && (predicate == null || predicate(child as T)))
                {
                    result.Add(child as T);
                }

                if (child.Children.Nothing())
                {
                    continue;
                }

                var res = child.FindActiveComponent<T>();
                res.SelectForeach(x => result.Add(x));
            }
            return result.Distinct();
        }

        public static T FindComponentByName<T>(this EditableComponent component, string name) where T : EditableComponent
        {
            return component.FirstOrDefault(x => x.Name == name && typeof(T).IsAssignableFrom(x.GetType())) as T;
        }

        public static void SetValue(this EditableComponent component, string name, object value)
        {
            var match = component.FirstOrDefault(x => x.FieldName == name);
            if (match is null)
            {
                return;
            }

            if (match is Textbox text)
            {
                text.Value = value;
            }
            else if (match is SearchEntry search && (value is null || value.GetType().IsInt32()))
            {
                search.Value = value?.ToString();
            }
            else if (match is Number number && (value is null || value.GetType().IsNumber()))
            {
                number.Value = Convert.ToDecimal(value);
            }
            else if (match is Checkbox checkbox && (value is null || value.GetType().IsBool()))
            {
                checkbox.Value = value as bool?;
            }
            else if (match is Datepicker dpk && (value is null || value.GetType().IsDate()))
            {
                dpk.Value = value as DateTime?;
            }
        }

        public static object GetValue(this EditableComponent com, bool simple = false)
        {
            if (com is Textbox text)
            {
                return text.Text;
            }
            else if (com is MultipleSearchEntry multiple)
            {
                return simple ? (object)multiple.ListValues.Combine() : multiple.ListValues;
            }
            else if (com is SearchEntry search)
            {
                return search.Value;
            }
            else if (com is Number number)
            {
                return number.Value;
            }
            else if (com is Checkbox chk)
            {
                return chk.Value;
            }
            else if (com is Datepicker dpk)
            {
                return dpk.Value;
            }
            else if (com is ImageUploader uploader)
            {
                return uploader.Path;
            }
            else if (com is CellText cellText)
            {
                return cellText.Element["innerText"];
            }
            return null;
        }

        public static void SetDisabled(this EditableComponent component, bool disabled)
        {
            if (component != null && component is EditableComponent editable)
            {
                editable.Disabled = disabled;
            }
        }

        public static void SetDisabled<T>(this EditableComponent component, string name, bool disabled) where T : EditableComponent
        {
            component = component.FirstOrDefault(x => x.Name == name && x is T) as T;
            if (component != null && component is EditableComponent editable)
            {
                editable.Disabled = disabled;
            }
        }

        public static void SetDataSourceSearchEntry(this EditableComponent component, string name, string DataSourceFilter)
        {
            var search = component.FirstOrDefault(x => x.Name == name && x is SearchEntry) as SearchEntry;
            if (search != null)
            {
                search.GuiInfo.DataSourceFilter = DataSourceFilter;
            }
        }

        public static void SetDataSourceGridView(this EditableComponent component, string name, string DataSourceFilter)
        {
            var search = component.FirstOrDefault(x => x.Name == name && x is GridView) as GridView;
            if (search != null)
            {
                search.DataSourceFilter = DataSourceFilter;
            }
        }

        public static T FindClosest<T>(this EditableComponent component) where T : class
        {
            var type = typeof(T);
            if (type.IsAssignableFrom(component.GetType()))
            {
                return component as T;
            }

            while (component.Parent != null)
            {
                component = component.Parent;
                if (type.IsAssignableFrom(component.GetType()))
                {
                    return component as T;
                }
            }
            return component as T;
        }

        public static T FindClosest<T>(this EditableComponent component, Func<T, bool> predicate) where T : EditableComponent
        {
            var type = typeof(T);
            if (type.IsAssignableFrom(component.GetType()) && predicate(component as T))
            {
                return component as T;
            }

            while (component.Parent != null)
            {
                component = component.Parent;
                if (type.IsAssignableFrom(component.GetType()) && predicate(component as T))
                {
                    return component as T;
                }
            }
            return component as T;
        }

        public static EditForm FindComponentEvent(this EditForm component, string eventName)
        {
            if (component is null)
            {
                return null;
            }

            var parent = component.ParentForm;
            while (parent != null && parent[eventName] == null)
            {
                parent = parent.ParentForm;
            }
            if (parent is null && component.Parent is EditForm parentForm)
            {
                parent = parentForm;
                while (parent != null && parent[eventName] == null)
                {
                    parent = parent.ParentForm;
                }
            }

            return parent;
        }

        public static void SetShow(this EditableComponent component, bool show, params string[] fieldNames)
        {
            if (component is null)
            {
                return;
            }

            component.FilterChildren(x => fieldNames.Contains(x.Name)).SelectForeach(x => x.Show = show);
        }

        public static void SetDisabled(this EditableComponent component, bool disabled, params string[] fieldNames)
        {
            if (component is null)
            {
                return;
            }

            component.FilterChildren<EditableComponent>(x => fieldNames.Contains(x.Name)).SelectForeach(x => x.Disabled = disabled);
        }

        public static void AlterPosition(this HTMLElement element, HTMLElement parentEle)
        {
            if (element is null || element.ParentElement is null || parentEle is null)
            {
                return;
            }
            var containerRect = parentEle.GetBoundingClientRect();
            var containerBottom = containerRect.Bottom;
            element.Style.Top = Auto;
            element.Style.Right = Auto;
            element.Style.Bottom = Auto;
            element.Style.Left = Auto;
            Html.Take(element).Floating(containerBottom, containerRect.Left);
            if (element.OutOfViewport().Right)
            {
                if (!element.OutOfViewport().Bottom)
                {
                    BottomCenter(element, parentEle);
                }
                else if (containerRect.Top > element.ClientHeight)
                {
                    TopCenter(element, parentEle);
                }
                else if (containerRect.Left > element.ClientWidth)
                {
                    LeftMiddle(element, parentEle);
                }
            }
            if (element.OutOfViewport().Bottom)
            {
                RightMiddle(element, parentEle);
                if (element.OutOfViewport().Right)
                {
                    LeftMiddle(element, parentEle);
                }
                if (element.OutOfViewport().Left)
                {
                    TopCenter(element, parentEle);
                }
            }
        }

        private static void BottomCenter(HTMLElement element, HTMLElement parent)
        {
            var containerRect = parent.GetBoundingClientRect();
            element.Style.Right = Auto;
            element.Style.Top = containerRect.Bottom + Utils.Pixel;
            MoveLeft(element);
        }

        public static decimal GetComputedPx(HTMLElement element, Func<CSSStyleDeclaration, string> prop)
        {
            var computedVal = prop.Invoke(Window.GetComputedStyle(element));
            return computedVal?.Replace(Utils.Pixel, string.Empty)?.TryParse<decimal>() ?? 0;
        }

        private static void TopCenter(HTMLElement element, HTMLElement parent)
        {
            element.Style.Right = Auto;
            MoveLeft(element);
            MoveTop(element, parent);
        }

        private static void MoveLeft(HTMLElement element)
        {
            while (element.OutOfViewport().Right)
            {
                var left = GetComputedPx(element, x => x.Left) - StepPx;
                element.Style.Left = left + Utils.Pixel;
            }
        }

        private static void MoveTop(HTMLElement element, HTMLElement parent = null)
        {
            var parentTop = parent?.GetBoundingClientRect()?.Top;
            while (element.OutOfViewport().Bottom || parent != null && element.GetBoundingClientRect().Bottom > parentTop)
            {
                var top = GetComputedPx(element, x => x.Top) - (parent is null ? StepPx : 1);
                element.Style.Top = top + Utils.Pixel;
            }
        }

        private static void LeftMiddle(HTMLElement element, HTMLElement parent)
        {
            var containerRect = parent.GetBoundingClientRect();
            element.Style.Left = Auto;
            element.Style.Bottom = Auto;
            element.Style.Right = containerRect.Left + Utils.Pixel;
            MoveTop(element);
        }

        private static void RightMiddle(HTMLElement element, HTMLElement parent)
        {
            var containerRect = parent.GetBoundingClientRect();
            element.Style.Right = Auto;
            element.Style.Bottom = Auto;
            element.Style.Left = containerRect.Right + Utils.Pixel;
            MoveTop(element);
        }

        public static IEnumerable<object> BuildGroupTree(IEnumerable<object> list, string[] groupKeys)
        {
            if (groupKeys.Nothing())
            {
                return list;
            }

            var firstKey = groupKeys.First();
            if (firstKey.IsNullOrWhiteSpace())
            {
                return list;
            }

            return list
                .GroupBy(x => x[firstKey])
                .Select(x => new GroupRowData
                {
                    Key = x.Key,
                    Children = BuildGroupTree(x.ToList(), groupKeys.Skip(1).ToArray()).ToList()
                })
                .Cast<object>();
        }

        public static void DownloadFile(string filename, object blob)
        {
            var a = Document.CreateElement("a") as HTMLAnchorElement;
            a.Style.Display = Display.None;
            /*@
            a.href = window.URL.createObjectURL(blob);
            a.download = filename;

            // Append anchor to body.
            document.body.appendChild(a);
            a.click();

            // Remove anchor from body
            document.body.removeChild(a);
             */
        }
    }
}

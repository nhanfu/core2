using Bridge.Html5;
using Core.Clients;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static async Task InvokeMethodAsync(this EditForm bl, string methodName, params object[] parameters)
        {
            var method = bl[methodName];
            if (method is null)
            {
                return;
            }

            using (Task task = null)
            {
                /*@
                var task = method.apply(bl, parameters);
                if (task == null || task.isCompleted == null) {
                    $tcs.setResult(null);
                    return;
                }
                */
                await task;
            }
        }

        public static async Task DispatchCustomEventAsync(this EditableComponent com, string events, CustomEventType eventType, params object[] parameters)
        {
            if (events.IsNullOrEmpty())
            {
                return;
            }

            var eventTypeName = eventType.ToString();
            await InvokeEventAsync(com, events, eventTypeName, parameters);
        }

        public static string MapToFilterOperator(this GridPolicy gp, string searchTerm)
        {
            if (searchTerm.IsNullOrWhiteSpace() || !gp.HasFilter || gp.FieldName.IsNullOrEmpty())
            {
                return string.Empty;
            }

            searchTerm = searchTerm.Trim();
            var fieldName = gp.FieldName.Replace(".", "/");
            if (gp.FieldName == "Id" ||
                gp.FieldName.Substring(gp.FieldName.Length - 2) == "Id" &&
                gp.FilterTemplate.IsNullOrEmpty())
            {
                return string.Empty;
            }

            if (gp.ComponentType == "Datepicker")
            {
                var parsedDate = DateTime.TryParseExact(searchTerm, "dd/MM/yyyy", CultureInfo.InvariantCulture, out var date);
                if (parsedDate)
                {
                    return $"{fieldName} eq cast({date.ToISOFormat()},Edm.DateTimeOffset)";
                }

                return string.Empty;
            }
            else if (gp.ComponentType == "Checkbox")
            {
                var parseBool = bool.TryParse(searchTerm, out bool val);
                if (!parseBool)
                {
                    return string.Empty;
                }

                return $"{fieldName} eq {val}";
            }
            else if (gp.ComponentType == "Number")
            {
                var parsedNumber = int.TryParse(searchTerm, out int searchNumber);
                if (!parsedNumber)
                {
                    return string.Empty;
                }

                return gp.FilterTemplate.HasAnyChar() ? string.Format(gp.FilterTemplate, searchNumber) : $"{fieldName} eq {searchNumber}";
            }
            return gp.FilterTemplate.HasAnyChar() ? string.Format(gp.FilterTemplate, searchTerm) : $"contains({fieldName}, '{searchTerm}')";
        }

        public static string FilterById(string searchTerm, IEnumerable<GridPolicy> headers)
        {
            if (searchTerm.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var searchQuery = string.Empty;
            var searchTermPattern = searchTerm.Replace(new RegExp(@"\d+"), string.Empty);
            if (searchTermPattern.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var idHeader = headers.FirstOrDefault(header =>
            {
                var format = header.FormatCell.IsNullOrEmpty() ? header.FormatRow : header.FormatCell;
                if (format.IsNullOrEmpty())
                {
                    return false;
                }

                var fieldPattern = format.Replace(new RegExp(@"\{[\s\S]*?\}"), string.Empty);
                return fieldPattern.ToLowerCase() == searchTermPattern.ToLowerCase();
            });
            if (idHeader != null)
            {
                var strId = Regex.Match(searchTerm, @"\d+").Value;
                if (strId.HasAnyChar())
                {
                    searchQuery = $"{idHeader.FieldName.Replace(".", "/")} eq {strId.TryParseInt() ?? 0}";
                }
            }
            return searchQuery;
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
            var feature = await LoadFeatureByName(featureName, publicForm: anonymous);
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

        public static Task<EditForm> InitFeatureByName(string hash, bool portal = true)
        {
            var tcs = new TaskCompletionSource<EditForm>();
            var featureName = hash.Replace("-", " ").Replace("#", string.Empty);
            Task.Run(async () =>
            {
                var feature = await LoadFeatureByName(featureName, true);
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

        public static async Task<Feature> LoadFeatureByName(string featureName, bool publicForm = false)
        {
#if RELEASE
            if (FeatureMap.ContainsKey(featureName))
            {
                return FeatureMap[featureName];
            }
#endif
            var prefix = publicForm ? "/Public/" : string.Empty;
            var featureOdata = new Client(nameof(Feature), typeof(User).Namespace).FirstOrDefaultAsync<Feature>(
                $"{prefix}?$filter=Active eq true and Name eq '{featureName}'", addTenant: true);
            var policyOdata = publicForm ? Task.FromResult(new List<FeaturePolicy>())
                : new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and Feature/Name eq '{featureName}'");
            var componentGroupTask = new Client(nameof(ComponentGroup), typeof(User).Namespace).GetRawList<ComponentGroup>(
                $"?$expand=Component($filter=Active eq true;$expand=Reference($select=Id,Name))" +
                $"&$filter=Active eq true and Feature/Name eq '{featureName}'", addTenant: true);
            await Task.WhenAll(featureOdata, policyOdata, componentGroupTask);
            var feature = featureOdata.Result;
            feature.FeaturePolicy = policyOdata.Result;
            feature.ComponentGroup = componentGroupTask.Result;
            FeatureMap.TryAdd(featureName, feature);
            ExecuteFeatureScript(feature);
            return feature;
        }

        public static async Task<Feature> LoadFeatureComponent(Feature feature1, bool publicForm = false)
        {
#if RELEASE
            if (FeatureMap.ContainsKey(featureName))
            {
                return FeatureMap[featureName];
            }
#endif
            var prefix = publicForm ? "/Public/" : string.Empty;
            var policyOdata = publicForm ? Task.FromResult(new List<FeaturePolicy>())
                : new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and FeatureId eq {feature1.Id}");
            var componentGroupTask = new Client(nameof(ComponentGroup), typeof(User).Namespace).GetRawList<ComponentGroup>(
                $"?$expand=Component($filter=Active eq true;$expand=Reference($select=Id,Name))" +
                $"&$filter=Active eq true and FeatureId eq {feature1.Id}", addTenant: true);
            await Task.WhenAll(policyOdata, componentGroupTask);
            feature1.FeaturePolicy = policyOdata.Result;
            feature1.ComponentGroup = componentGroupTask.Result;
            FeatureMap.TryAdd(feature1.Name, feature1);
            ExecuteFeatureScript(feature1);
            return feature1;
        }

        public static async Task<Feature> LoadFeatureByNameOrViewClass(string nameOrViewClass)
        {
            var exists = FeatureMap.Values.FirstOrDefault(x => x.ViewClass == nameOrViewClass);
            if (exists != null)
            {
                return exists;
            }
            var featureTask = new Client(nameof(Feature), typeof(User).Namespace).FirstOrDefaultAsync<Feature>(
                $"?$expand=Entity($select=Name)&$filter=Active eq true and (Name eq '{nameOrViewClass}' or ViewClass eq '{nameOrViewClass}')");
            var policyTask = new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and (Feature/Name eq '{nameOrViewClass}' or Feature/ViewClass eq '{nameOrViewClass}')");
            var componentGroupTask = new Client(nameof(ComponentGroup), typeof(User).Namespace).GetRawList<ComponentGroup>(
                $"?$expand=Component($filter=Active eq true;$expand=Reference($select=Id,Name))" +
                $"&$filter=Active eq true and (Feature/Name eq '{nameOrViewClass}' or Feature/ViewClass eq '{nameOrViewClass}')");
            await Task.WhenAll(featureTask, policyTask, componentGroupTask);
            var feature = featureTask.Result;
            feature.FeaturePolicy = policyTask.Result;
            feature.ComponentGroup = componentGroupTask.Result;
            FeatureMap.TryAdd(feature.Name, feature);
            ExecuteFeatureScript(feature);
            return feature;
        }

        public static async Task<Feature> LoadEditorFeatureByNameByEntity(int entity)
        {
            var exists = FeatureMap.Values.FirstOrDefault(x => x.EntityId == entity && x.Name.IndexOf("editor", StringComparison.OrdinalIgnoreCase) >= 0);
            if (exists != null)
            {
                return exists;
            }
            var featureTask = new Client(nameof(Feature), typeof(User).Namespace).FirstOrDefaultAsync<Feature>(
                $"?$expand=Entity($select=Name)&$filter=Active eq true and EntityId eq {entity}  ");
            var policyTask = new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and Feature/EntityId eq {entity}");
            var componentGroupTask = new Client(nameof(ComponentGroup), typeof(User).Namespace).GetRawList<ComponentGroup>(
                $"?$expand=Component($filter=Active eq true;$expand=Reference($select=Id,Name))" +
                $"&$filter=Active eq true and Feature/EntityId eq {entity} ");
            await Task.WhenAll(featureTask, policyTask, componentGroupTask);
            var feature = featureTask.Result;
            feature.FeaturePolicy = policyTask.Result;
            feature.ComponentGroup = componentGroupTask.Result;
            FeatureMap.TryAdd(feature.Name, feature);
            ExecuteFeatureScript(feature);
            return feature;
        }

        public static async Task<List<FeaturePolicy>> LoadRecordPolicy(int[] ids, int entity)
        {
            if (ids.Nothing() || ids.All(x => x <= 0))
            {
                return new List<FeaturePolicy>();
            }
            return await new Client(nameof(FeaturePolicy), typeof(User).Namespace).GetRawList<FeaturePolicy>(
                $"?$filter=Active eq true and EntityId eq {entity} and RecordId in ({ids.Combine(",")})");
        }

        private static void ExecuteFeatureScript(Feature feature)
        {
            if (feature.ViewClass.IsNullOrWhiteSpace())
            {
                return;
            }

            var type = Type.GetType(feature.ViewClass);
            if (type != null)
            {
                return;
            }

            var script = Document.CreateElement(Bridge.Html5.ElementType.Script.ToString()) as HTMLScriptElement;
            script.TextContent = feature.Script;
            script.Type = "text/javascript";
            Document.Head.AppendChild(script);
            script.Remove();
        }

        public static async Task<EditableComponent> AddChild(this EditableComponent com, string id, string featureName, string className)
        {
            await LoadFeatureByName(featureName);
            var type = Type.GetType(className);
            var instance = Activator.CreateInstance(type) as EditableComponent;
            instance.Id = id;
            com.AddChild(instance);
            return instance;
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
                res.ForEach(x => result.Add(x));
            }
            return result.Distinct();
        }

        public static T FindComponentByName<T>(this EditableComponent component, string name) where T : EditableComponent
        {
            return component.FirstOrDefault(x => x.Name == name && typeof(T).IsAssignableFrom(x.GetType())) as T;
        }

        public static void SetValue(this EditableComponent component, string name, object value)
        {
            var match = component.FirstOrDefault(x => x.GuiInfo?.FieldName == name);
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
                search.Value = (int?)value;
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

        public static void SetAutoWidth(this EditableComponent component, string text, string font, int padding = 8)
        {
            component.ParentElement.Style.MinWidth = text.TextWidth(font) + 8 + "px";
            component.ParentElement.Style.MaxWidth = text.TextWidth(font) + 8 + "px";
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

        public static void SetDataSourceSearchEntry(this EditableComponent component, string name, string datasource)
        {
            var search = component.FirstOrDefault(x => x.Name == name && x is SearchEntry) as SearchEntry;
            if (search != null)
            {
                search.DataSourceFilter = datasource;
            }
        }

        public static void SetDataSourceGridView(this EditableComponent component, string name, string datasource)
        {
            var search = component.FirstOrDefault(x => x.Name == name && x is GridView) as GridView;
            if (search != null)
            {
                search.DataSourceFilter = datasource;
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

            component.FilterChildren(x => fieldNames.Contains(x.Name)).ForEach(x => x.Show = show);
        }

        public static void SetDisabled(this EditableComponent component, bool disabled, params string[] fieldNames)
        {
            if (component is null)
            {
                return;
            }

            component.FilterChildren<EditableComponent>(x => fieldNames.Contains(x.Name)).ForEach(x => x.Disabled = disabled);
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

        public static bool CheckValidity(this EditableComponent com, bool showMessage = true)
        {
            var invalidFields = com.GetInvalid();
            if (invalidFields.Nothing())
            {
                return true;
            }

            if (showMessage)
            {
                invalidFields.ForEach(x => { x.Disabled = false; });
                invalidFields.FirstOrDefault().Focus();
                var message = string.Join("<br />", invalidFields.SelectMany(x => x.ValidationResult.Values));
                Toast.Warning(message);
            }
            return false;
        }
    }
}

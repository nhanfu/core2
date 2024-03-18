using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components.Framework
{
    public class NotificationBL : EditableComponent
    {
        private const string NoMoreTask = "No more task";
        private static NotificationBL _instance;
        private static Observable<string> _countNtf;
        private static Observable<string> _countUser;
        private HTMLElement _task;
        private HTMLElement _countBadge;
        public static ObservableList<TaskNotification> Notifications { get; set; }
        public static ObservableList<User> UserActive { get; set; }
        private Token CurrentUser { get; set; }

        protected NotificationBL() : base(null)
        {
            Notifications = new ObservableList<TaskNotification>();
            UserActive = new ObservableList<User>();
            _countNtf = new Observable<string>();
            _countUser = new Observable<string>();
            Window.AddEventListener("task", ProcessIncomMessage);
        }

        public void ProcessIncomMessage(object obj)
        {
            if (obj is null)
            {
                return;
            }

            var task = (TaskNotification)obj;
            if (task is null)
            {
                return;
            }

            var existTask = Notifications.Data.FirstOrDefault(x => x.Id == task.Id);
            if (existTask == null)
            {
                Notifications.Add(task, 0);
                ToggleBageCount(Notifications.Data.Count);
            }
            SetBadgeNumber();
            var entity = Utils.GetEntityById(task.EntityId);
            task.Entity = new Entity { Name = entity.Name };
            /*@
            if (typeof(Notification) !== 'undefined' && Notification.permission === "granted") {
                this.ShowNativeNtf(task);
            } else if (typeof(Notification) !== 'undefined' && Notification.permission !== "denied") {
                Notification.requestPermission().then((permission) => {
                    if (permission !== 'granted') {
                        this.ShowToast(task);
                    }
                    else this.ShowNativeNtf(task);
                });
            } else this.ShowToast(task);
            */
        }

        private int SetBadgeNumber()
        {
            var unreadCount = Notifications.Data.Count(x => x.StatusId == ((int)TaskStateEnum.UnreadStatus).ToString());
            _countNtf.Data = unreadCount > 9 ? "9+" : unreadCount.ToString();
            var badge = unreadCount > 9 ? 9 : unreadCount;
            /*@
            if (typeof(cordova) !== 'undefined' &&
                typeof(cordova.plugins) !== 'undefined' &&
                typeof(cordova.plugins.notification) !== 'undefined') {
                cordova.plugins.notification.badge.requestPermission(function (granted) {
                    cordova.plugins.notification.badge.set(unreadCount);
                });
            }
            */
            return badge;
        }

        private void ShowNativeNtf(TaskNotification task)
        {
            if (task is null)
            {
                return;
            }

            /*@
            var nativeNtf = new Notification(task.Title,
            {
                body: task.Description,
                icon: task.Attachment,
                vibrate: [200, 100, 200],
                badge: "./favicon.ico"
            });
            nativeNtf.addEventListener('click', () => this.OpenNotification(task));
            setTimeout(() =>
            {
                nativeNtf.close();
            }, 7000);
            */
        }

        public static NotificationBL Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new NotificationBL();
                }
                return _instance;
            }
        }

        private static readonly HTMLElement NotiRoot = Document.GetElementById("notification-list");
        private static readonly HTMLElement ProfileRoot = Document.GetElementById("profile-info1");

        public override void Render()
        {
            Html.Take(NotiRoot).Clear();
            Html.Take("#user-active").Clear();
            SetBadgeNumber();
            CurrentUser = Client.Token;
            if (CurrentUser is null) return;
            CurrentUser.Avatar = (CurrentUser.Avatar.Contains("://") ? "" : Client.Origin) + (CurrentUser.Avatar.IsNullOrWhiteSpace() ? "./image/chinese.jfif" : CurrentUser.Avatar);
            RenderNotification();
            RenderProfile(ProfileRoot);
            var xhr = new XHRWrapper 
            {
                Url = $"/GetUserActive",
                Method = HttpMethod.POST,
                ShowError = false
            };
            Client.Instance.SubmitAsync<List<User>>(xhr)
                .Done(x => UserActive.Data = x)
                .Catch(Console.WriteLine);
        }

        public void RenderProfile(HTMLElement profileRoot)
        {
            if (profileRoot is null) return;
            var isSave = Window.LocalStorage.GetItem("isSave");
            Html.Take(profileRoot).Clear();
            var html = Html.Take(profileRoot);
            html.A.ClassName("navbar-nav-link d-flex align-items-center dropdown-toggle").DataAttr("toggle", "dropdown").Span.ClassName("text-truncate").Text(CurrentUser.TenantCode + ": " + CurrentUser.FullName).EndOf(ElementType.a)
                .Div.ClassName("dropdown-menu dropdown-menu-right notClose mt-0 border-0").Style("border-top-left-radius: 0;border-top-right-radius: 0")
                    .A.ClassName("dropdown-item").Event(EventType.Click, ViewProfile).I.ClassName("far fa-user").End.Text("Account (" + CurrentUser.TenantCode + ": " + CurrentUser.FullName + ")").EndOf(ElementType.a);
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            var langSelect = new LangSelect(new Component(), html.GetContext());
            langSelect.Render();
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            html.A.Event(EventType.Click, DarkOrLightModeSwitcher).ClassName("dropdown-item").I.ClassName("far fa-moon-cloud").End.Text("Dark/Light Mode").EndOf(ElementType.a);
            html.A.Event(EventType.Click, SignOut).ClassName("dropdown-item").I.ClassName("far fa-power-off").End.Text("Logout").EndOf(ElementType.a);
        }

        private void SignOut(Event e)
        {
            e.PreventDefault();
            var task = Client.Instance.PostAsync<bool>(Client.Token, "/user/signOut");
            task.Done(res =>
            {
                Client.SignOutEventHandler?.Invoke();
                Client.Token = null;
                EditForm.NotificationClient?.Close();
                Window.SetTimeout(() => Window.Location.Reload(), 1000);
            });
        }

        private void DarkOrLightModeSwitcher(Event e)
        {
            e.PreventDefault();
            var htmlElement = Document.DocumentElement;
            htmlElement.Style["filter"] = htmlElement.Style["filter"].ToString().Contains("(1)") ? "invert(0)" : "invert(1)";
        }

        private void ViewProfile(Event e)
        {
            e.PreventDefault();
            this.OpenTab(id: "User" + Client.Token.UserId,
                featureName: "UserProfile",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.User.UserProfileBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    return instance;
                }).Done();
            Dispose();
        }

        private void RenderNotification()
        {
            if (NotiRoot is null) return;
            var html = Html.Take(NotiRoot).A.ClassName("navbar-nav-link").DataAttr("toggle", "dropdown").I.ClassName("far fa-bell fa-lg").EndOf(ElementType.i);
            if (_countNtf.Data != string.Empty)
            {
                html.Span.ClassName("badge badge-pill bg-warning-400 ml-auto ml-md-0").Text(_countNtf);
                _countBadge = Html.Context;
            };
            html.EndOf(ElementType.a);
            html.Div.Style("border-top-left-radius: 0;border-top-right-radius: 0")
                .ClassName("dropdown-menu dropdown-menu-right dropdown-content wmin-md-300 mt-0")
                .Style("border-top-left-radius: 0; border-top-right-radius: 0");
            html.Div.ForEach(Notifications, (task, index) =>
            {
                RenderTask(task);
            }).End.Render();
            html.A.ClassName("dropdown-item dropdown-footer").Event(EventType.Click, SeeMore).Text("See more").EndOf(ElementType.a);
        }

        private void RenderTask(TaskNotification task)
        {
            if (task is null)
            {
                return;
            }

            var className = task.StatusId == ((int)TaskStateEnum.UnreadStatus).ToString() ? "text-danger" : "text-muted";
            Html.Instance.A.ClassName("dropdown-item").Div.ClassName("media").Event(EventType.Click, (e) =>
            {
                OpenNotification(task);
            })
            .Div.ClassName("media-body").H3.ClassName("dropdown-item-title").Text(task.Title).Span.ClassName("float-right text-sm " + className).I.ClassName("fas fa-star").End.End.End
            .P.ClassName("text-sm").Text(task.Description).End
            .P.ClassName("text-sm text-muted")
                .I.ClassName("far fa-clock mr-1").End.Text(task.Deadline.ToString("dd/MM/yyyy HH:mm")).EndOf(ElementType.a);
        }

        private void ToggleBageCount(int count)
        {
            _countBadge.Style.Display = count == 0 ? Display.None : Display.InlineBlock;
        }

        private void SeeMore(Event e)
        {
            e.PreventDefault();
            var lastSeenTask = Notifications.Data.OrderByDescending(x => x.InsertedDate).FirstOrDefault();
            if (lastSeenTask is null) {
                Toast.Warning(NoMoreTask);
                return;
            }
            Spinner.AppendTo(Element, timeout: 250);
            var lastSeenDateStr = lastSeenTask.InsertedDate.ToISOFormat();
            var sql = new SqlViewModel
            {
                ComId = "Task",
                Action = "SeeMore",
                MetaConn = Client.MetaConn,
                DataConn = Client.DataConn,
                Params = JSON.Stringify(new { Date = lastSeenDateStr })
            };
            Client.Instance.UserSvc(sql).Done(ds =>
            {
                var olderItems = ds.Length > 0 ? ds[0].Select(x => x.CastProp<TaskNotification>()).ToArray() : null;
                if (olderItems.Nothing()) 
                {
                    Toast.Warning(NoMoreTask);
                    return;
                }
                var taskList = Notifications.Data.Union(olderItems).ToList();
                Notifications.Data = taskList;
            }).Catch(err => {
                Toast.Warning(err?.Message ?? NoMoreTask);
            });
        }

        public void MarkAllAsRead(Event e)
        {
            e.PreventDefault();
            Client.Instance.PostAsync<bool>(null, nameof(TaskNotification) + "/MarkAllAsRead")
            .Done(res =>
            {
                ToggleBageCount(Notifications.Data.Count(x => x.StatusId == ((int)TaskStateEnum.UnreadStatus).ToString()));
                foreach (var task in _task.QuerySelectorAll(".task-unread"))
                {
                    task.ReplaceClass("task-unread", "task-read");
                }
            });
        }

        public void OpenNotification(TaskNotification notification)
        {
            MarkAsRead(notification);
        }

        protected override void RemoveDOM()
        {
            Html.Take("#notification").Clear();
        }

        private void MarkAsRead(TaskNotification task)
        {
            task.StatusId = ((int)TaskStateEnum.Read).ToString();
            Client.Instance.PatchAsync(task.MapToPatch())
                .Done(x => SetBadgeNumber());
        }

        public override void Dispose()
        {
            _task.AddClass("hide");
        }
    }
}
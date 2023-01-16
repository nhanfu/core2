using Bridge.Html5;
using Core.ViewModels;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;
using Core.Fw.Authentication;
using ElementType = Core.MVVM.ElementType;
using Notification = Retyped.dom.Notification;
using System.Collections.Generic;

namespace Core.Notifications
{
    public class NotificationBL : EditableComponent
    {
        protected static NotificationBL _instance;
        private static Observable<string> _countNtf;
        private HTMLElement _profile;
        private HTMLElement _task;
        private HTMLElement _countBadge;

        public static ObservableList<TaskNotification> Notifications { get; private set; }
        private Token CurrentUser { get; set; }

        protected NotificationBL() : base(null)
        {
            Notifications = new ObservableList<TaskNotification>();
            _countNtf = new Observable<string>();
            EditForm.NotificationClient?.AddListener(nameof(TaskNotification), ProcessIncomMessage);
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
                ToggleBageCount(Notifications.Data.Count(x => x.StatusId == (int)TaskStateEnum.UnreadStatus));
                PopupNotification(task);
            }
            SetBadgeNumber();
            var entity = Utils.GetEntity(task.EntityId ?? 0);
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
            var unreadCount = Notifications.Data.Count(x => x.StatusId == (int)TaskStateEnum.UnreadStatus);
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

            Notification nativeNtf = null;
            /*@
            var nativeNtf = new Notification(task.Title,
            {
                body: task.Description,
                icon: task.Attachment,
                vibrate: [200, 100, 200],
                badge: "./favicon.ico"
            });
            nativeNtf.addEventListener('click', () => this.OpenNotification(task));
            */
            Window.SetTimeout(() =>
            {
                nativeNtf.close();
            }, 7000);
        }

        private void ShowToast(TaskNotification task)
        {
            Toast.Success($"Thông báo từ hệ thống <br /> {task.Title} - {task.Description}");
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

        public override void Render()
        {
            Task.Run(RenderAsync);
        }

        public async Task RenderAsync()
        {
            Html.Take("#notification").Clear();
            var notifications = await new Client(nameof(TaskNotification)).GetRawList<TaskNotification>($"?$expand=Entity&$orderby=InsertedDate desc&$top=50");
            Notifications.Data = notifications;
            SetBadgeNumber();
            CurrentUser = Client.Token;
            CurrentUser.Avatar = Client.Origin + (CurrentUser.Avatar.IsNullOrWhiteSpace() ? "./image/chinese.jfif" : CurrentUser.Avatar);
            RenderNotification();
            RenderProfile();
        }

        public void RenderProfile()
        {
            var html = Html.Instance;
            Html.Take("#notification .app-bar-container").A.ClassName("app-bar-item")
                .Event(EventType.Click, ShowProfile)
                .Img.Src(CurrentUser.Avatar).Attr("alt", "Avatar").ClassName("user-image")
                    .Id("user-image").End.Span.ClassName("hidden-xs").Id("name-user").Text(CurrentUser.FullName)
                .EndOf(ElementType.a);
            html.Div.ClassName("pos-relative fix-pos-relative shadow").TabIndex(-1).Div.ClassName("fg-black");
            _profile = Html.Context.ParentElement;
            _profile.Style.Display = Display.None;
            _profile.AddEventListener(EventType.FocusOut, () => _profile.Style.Display = Display.None);
            html.Ul.ClassName("ul-Warning")
                .Li.ClassName("user-header").Img.Src(CurrentUser.Avatar).Attr("alt", "Avatar")
                .ClassName("img-circle").Id("img-detail").End
                .P.Id("Username-text").Text($"{Client.Token.TenantCode} {CurrentUser.FullName}").End.Span.Id("text-address").Text(CurrentUser.Address ?? "").End
                .EndOf(".user-header")
                .Li.ClassName("user-footer").Span.AsyncEvent(EventType.Click, ViewProfile).IText("Trang cá nhân").EndOf(ElementType.span);
            var langSelect = new LangSelect(null, null) { ParentElement = Html.Context };
            langSelect.Render();
            html.Span.ClassName("pull-right").AsyncEvent(EventType.Click, SignOut).IText("Đăng xuất");
        }

        private void ShowProfile()
        {
            _profile.Style.Display = Display.Block;
            _profile.Focus();
        }

        private async Task SignOut(Event e)
        {
            e.PreventDefault();
            var client = new Client(nameof(User));
            await client.CreateAsync<bool>(Client.Token, "SignOut");
            Client.SignOutEventHandler?.Invoke();
            Client.Token = null;
            EditForm.NotificationClient?.Close();
            await Task.Delay(1000);
            Window.Location.Reload();
        }

        private async Task ViewProfile(Event e)
        {
            e.PreventDefault();
            await this.OpenTab(id: "User" + Client.Token.UserId,
                featureName: "UserProfile",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.User.UserProfileBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    return instance;
                });
            Dispose();
        }

        private void RenderNotification()
        {
            var html = Html.Take("#notification").Div.ClassName("app-bar-container");
            html.A.ClassName("app-bar-item").Event(EventType.Click, ToggleNotification);
            html.Span.ClassName("fas fa-bell");
            html.Span.ClassName("badge bg-orange fg-white").Id("span-count").Text(_countNtf);
            _countBadge = Html.Context;
            ToggleBageCount(Notifications.Data.Count(x => x.StatusId == (int)TaskStateEnum.UnreadStatus));
            html.EndOf(ElementType.a);
            html.Div.ClassName("pos-relative fix-pos-relative hide shadow").TabIndex(-1);
            _task = Html.Context;
            _task.AddEventListener(EventType.FocusOut, () => _task.Style.Display = Display.None);
            html.DataAttr("role", "dropdown");
            html.Div.ClassName("fg-black");
            html.Ul.ClassName("ul-Warning");
            html.Li.ClassName("header1").Span.Id("span-count-liabilitiesWarning").Text(_countNtf).End.IText(" thông báo mới !").End.Render();
            html.Li.ClassName("li-Root")
                .Ul.ClassName("menu")
                    .ForEach(Notifications, (task, index) =>
                    {
                        if (task is null)
                        {
                            return;
                        }

                        var className = task.StatusId == (int)TaskStateEnum.UnreadStatus ? "task-unread" : "task-read";
                        html.Li.ClassName("task " + className);
                        var li = Html.Context;
                        html.Div.ClassName("a-items").Event(EventType.Click, async () =>
                        {
                            await OpenNotification(task);
                            li.ReplaceClass("task-unread", "task-read");
                        });
                        html.Div.ClassName("pull-left").I.ClassName(task.Attachment + " text-red").EndOf(".pull-left");
                        html.H4.ClassName("h4-items").IText(task.Title).End.Render();
                        html.P.ClassName("p-warning").Text(task.Deadline.DateString()).End
                        .P.ClassName("p-warning").IText(task.Description).EndOf(ElementType.li);
                    });

            Html.Instance.EndOf(ElementType.ul).Ul.ClassName("footer-viewall").Li
                    .Span.Text("Xem thêm")
                    .AsyncEvent(EventType.Click, SeeMore).End
                    .Span.Text("Đọc tất cả")
                    .AsyncEvent(EventType.Click, MarkAllAsRead);
            Notifications.Data.ForEach(PopupNotification);
        }

        private void ToggleBageCount(int count)
        {
            _countBadge.Style.Display = count == 0 ? Display.None : Display.InlineBlock;
        }

        private void PopupNotification(TaskNotification task)
        {
            if (task.StatusId != (int)TaskStateEnum.UnreadStatus)
            {
                return;
            }

            if (task.EntityId == Utils.GetEntity("CoordinationDetail")?.Id
                && (Client.CheckHasRole(RoleEnum.Driver_Cont) || Client.CheckHasRole(RoleEnum.Driver_Truck)))
            {
                task.StatusId = (int)TaskStateEnum.Read;
                var count = DateTime.Now - task.Deadline;
                var (days, hour, minute) = count.ToDayHourMinute();
                var dayStr = days > 0 ? (days + " ngày") : string.Empty;
                var confirm = new ConfirmDialog()
                {
                    Title = $"<b>Thông báo</b> - <span class=\"text-small\">{dayStr} {hour}h {minute} phút trước</span>",
                    Content = $"{task.Title}<br />{task.Description}",
                    IgnoreNoButton = true,
                    YesText = "Đồng ý",
                    CancelText = "Đóng"
                };
                confirm.YesConfirmed += async () => await OpenNotification(task);
                confirm.Canceled += async () => await MarkAsRead(task);
                confirm.Render();
            }
        }

        private void ToggleNotification()
        {
            _task.Style.Display = Display.Block;
            _task.Focus();
        }

        private async Task SeeMore(Event e)
        {
            var lastSeenTask = Notifications.Data.LastOrDefault();
            var lastSeenDate = lastSeenTask?.InsertedDate ?? DateTime.Now;
            var olderTasks = await new Client(nameof(TaskNotification)).GetRawList<TaskNotification>(
                $"?$filter=InsertedDate lt {lastSeenDate.ToISOFormat()}&$expand=Entity&$orderby=InsertedDate desc&$top=50");
            var taskList = Notifications.Data.Union(olderTasks).ToList();
            Notifications.Data = taskList;
        }

        private async Task MarkAllAsRead(Event e)
        {
            e.PreventDefault();
            Client client = new Client(nameof(TaskNotification));
            var res = await client.PostAsync<bool>(null, "MarkAllAsRead");
            ToggleBageCount(Notifications.Data.Count(x => x.StatusId == (int)TaskStateEnum.UnreadStatus));
            _task.QuerySelectorAll(".task-unread").ForEach(task =>
            {
                task.ReplaceClass("task-unread", "task-read");
            });
        }

        public async Task OpenNotification(TaskNotification notification)
        {
            await MarkAsRead(notification);
            await OpenTaskFeature(notification, this);
        }

        public virtual async Task OpenTaskFeature(TaskNotification notification, EditableComponent baseComponent)
        {
            // Not to do anything
        }

        protected override void RemoveDOM()
        {
            Html.Take("#notification").Clear();
        }

        private async Task MarkAsRead(TaskNotification task)
        {
            task.StatusId = (int)TaskStateEnum.Read;
            await new Client(nameof(TaskNotification)).UpdateAsync<TaskNotification>(task);
            SetBadgeNumber();
        }

        public override void Dispose()
        {
            _task.AddClass("hide");
        }
    }
}
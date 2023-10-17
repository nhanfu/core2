using Bridge;
using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using ElementType = Core.MVVM.ElementType;
using Notification = Retyped.dom.Notification;

namespace TMS.UI.Notifications
{
    public class NotificationBL : EditableComponent
    {
        private static NotificationBL _instance;
        private static Observable<string> _countNtf;
        private static Observable<string> _countUser;
        private HTMLElement _profile;
        private HTMLElement _task;
        private HTMLElement _countBadge;
        public static ObservableList<TaskNotification> Notifications { get; private set; }
        public static ObservableList<User> UserActive { get; set; }
        private Token CurrentUser { get; set; }

        protected NotificationBL() : base(null)
        {
            Notifications = new ObservableList<TaskNotification>();
            UserActive = new ObservableList<User>();
            _countNtf = new Observable<string>();
            _countUser = new Observable<string>();
            EditForm.NotificationClient?.AddListener(nameof(TaskNotification), ((int)TypeEntityAction.UpdateEntity).ToString(), ProcessIncomMessage);
            EditForm.NotificationClient?.AddListener(nameof(TaskNotification), ((int)TypeEntityAction.MessageCountBadge).ToString(), ProcessIncomMessage);

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
                PopupNotification(task);
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
            Task.Run(async () =>
            {
                if (task.EntityId == Utils.GetEntity(nameof(Entity)).Id)
                {
                    /*@
                     Swal.fire({
                          icon: 'error',
                          title: 'Hệ thống sẽ cập nhật sau 1 phút',
                          text: 'Bạn có thể xử lý công việc còn lại trong 1 phút kể từ lúc này',
                          footer: '<a href="#">Vui lòng không ctrl+f5 cảm ơn!</a>'
                        })
                     */
                    await Task.Delay(1000 * 60);
                    /*@
                     let timerInterval
                        Swal.fire({
                          title: 'Hệ thống đang cập nhật vui lòng chờ trong giây lát!',
                          html: 'Chúng tôi sẽ khởi động lại sau <b></b> giây.',
                          timer: 1000*60*3,
                          allowOutsideClick: false,
                          timerProgressBar: true,
                          didOpen: () => {
                            Swal.showLoading()
                            const b = Swal.getHtmlContainer().querySelector('b')
                            timerInterval = setInterval(() => {
                              b.textContent = (Swal.getTimerLeft()/1000).toFixed(0)
                            }, 1000)
                          },
                          willClose: () => {
                            clearInterval(timerInterval)
                          }
                        }).then((result) => {
                             if (result.dismiss === Swal.DismissReason.timer)
                             {
                                window.location.reload(true);
                             }
                       })
                    */
                }
                else
                {
                    Toast.Success($"Thông báo từ hệ thống <br /> {task.Title} - {task.Description}");
                }
            });
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
            Html.Take("#notification-list").Clear();
            Html.Take("#user-active").Clear();
            var notifications = new Client(nameof(TaskNotification)).GetRawList<TaskNotification>($"?$expand=Entity&$orderby=InsertedDate desc&$top=50");
            var userActive = new Client(nameof(TaskNotification)).PostAsync<List<User>>(null, $"GetUserActive");
            await Task.WhenAll(notifications, userActive);
            Notifications.Data = notifications.Result;
            UserActive.Data = userActive.Result;
            SetBadgeNumber();
            CurrentUser = Client.Token;
            CurrentUser.Avatar = (CurrentUser.Avatar.Contains("://") ? "" : Client.Origin) + (CurrentUser.Avatar.IsNullOrWhiteSpace() ? "./image/chinese.jfif" : CurrentUser.Avatar);
            RenderNotification();
            RenderProfile(".profile-info1");
        }

        public void RenderProfile(string classname)
        {
            var isSave = Window.LocalStorage.GetItem("isSave");
            Html.Take(classname).Clear();
            var html = Html.Take(classname);
            html.A.ClassName("navbar-nav-link d-flex align-items-center dropdown-toggle").DataAttr("toggle", "dropdown").Span.ClassName("text-truncate").Text(CurrentUser.TenantCode + ": " + CurrentUser.FullName).EndOf(ElementType.a)
                .Div.ClassName("dropdown-menu dropdown-menu-right notClose mt-0 border-0").Style("border-top-left-radius: 0;border-top-right-radius: 0")
                    .A.ClassName("dropdown-item").AsyncEvent(EventType.Click, ViewProfile).I.ClassName("far fa-user").End.Text("Account (" + CurrentUser.TenantCode + ": " + CurrentUser.FullName + ")").EndOf(ElementType.a);
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            var langSelect = new LangSelect(new Core.Models.Component(), html.GetContext());
            langSelect.Render();
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            html.A.AsyncEvent(EventType.Click, SignOut).ClassName("dropdown-item").I.ClassName("far fa-power-off").End.Text("Logout").EndOf(ElementType.a);
            Html.Take(".btn-logout").AsyncEvent(EventType.Click, SignOut);
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
            var html = Html.Take("#notification-list").A.ClassName("navbar-nav-link").DataAttr("toggle", "dropdown").I.ClassName("far fa-bell fa-lg").EndOf(ElementType.i);
            if (_countNtf.Data != string.Empty)
            {
                html.Span.ClassName("badge badge-pill bg-warning-400 ml-auto ml-md-0").Text(_countNtf);
                _countBadge = Html.Context;
            };
            html.EndOf(ElementType.a);
            html.Div.Style("border-top-left-radius: 0;border-top-right-radius: 0").ClassName("dropdown-menu dropdown-menu-right dropdown-content wmin-md-300 mt-0").Style("border-top-left-radius: 0;border-top-right-radius: 0");
            html.ForEach(Notifications, (task, index) =>
            {
                if (task is null)
                {
                    return;
                }

                var className = task.StatusId == ((int)TaskStateEnum.UnreadStatus).ToString() ? "text-danger" : "text-muted";
                html.A.ClassName("dropdown-item").Div.ClassName("media").Event(EventType.Click, async (e) =>
                {
                    await OpenNotification(task);
                })
                .Div.ClassName("media-body").H3.ClassName("dropdown-item-title").Text(task.Title).Span.ClassName("float-right text-sm " + className).I.ClassName("fas fa-star").End.End.End
                .P.ClassName("text-sm").Text(task.Description).End
                .P.ClassName("text-sm text-muted")
                    .I.ClassName("far fa-clock mr-1").End.Text(task.Deadline.ToString("dd/MM/yyyy HH:mm")).EndOf(ElementType.a);
            });
            html.A.ClassName("dropdown-item dropdown-footer").AsyncEvent(EventType.Click, SeeMore).Text("See more").EndOf(ElementType.a);
            Notifications.Data.ForEach(PopupNotification);
        }

        private void ToggleBageCount(int count)
        {
            _countBadge.Style.Display = count == 0 ? Display.None : Display.InlineBlock;
        }

        private void PopupNotification(TaskNotification task)
        {
            if (task.StatusId != ((int)TaskStateEnum.UnreadStatus).ToString())
            {
                return;
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
            ToggleBageCount(Notifications.Data.Count(x => x.StatusId == ((int)TaskStateEnum.UnreadStatus).ToString()));
            foreach (var task in _task.QuerySelectorAll(".task-unread"))
            {
                task.ReplaceClass("task-unread", "task-read");
            }
        }

        public async Task OpenNotification(TaskNotification notification)
        {
            await MarkAsRead(notification);
            await OpenTaskFeature(notification, this);
        }

        protected override void RemoveDOM()
        {
            Html.Take("#notification").Clear();
        }

        private async Task MarkAsRead(TaskNotification task)
        {
            task.StatusId = ((int)TaskStateEnum.Read).ToString();
            await new Client(nameof(TaskNotification)).UpdateAsync<TaskNotification>(task);
            SetBadgeNumber();
        }

        public override void Dispose()
        {
            _task.AddClass("hide");
        }

        public async Task OpenTaskFeature(TaskNotification notification, EditableComponent baseComponent)
        {
        }
    }
}
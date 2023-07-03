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
using static Retyped.canvasjs.CanvasJS;
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
        private static ObservableList<Convertation> Convertations { get; set; }
        private static Observable<Convertation> Convertation { get; set; }
        private static ObservableList<Chat> Chats { get; set; }
        private Token CurrentUser { get; set; }

        private NotificationBL() : base(null)
        {
            Notifications = new ObservableList<TaskNotification>();
            UserActive = new ObservableList<User>();
            Convertations = new ObservableList<Convertation>();
            Convertation = new Observable<Convertation>();
            Chats = new ObservableList<Chat>();
            _countNtf = new Observable<string>();
            _countUser = new Observable<string>();
            EditForm.NotificationClient?.AddListener(nameof(TaskNotification), ProcessIncomMessage);
            EditForm.NotificationClient?.AddListener(nameof(Chat), GetChat);
        }

        private void GetChat(object arg)
        {
            if (arg is null)
            {
                return;
            }
            var center = Document.QuerySelector(".center");
            if (center.HasClass("d-none"))
            {
                center.RemoveClass("d-none");
            }
            var chat = arg as Chat;
            if (Convertation.Data != null && chat.ConvertationId == Convertation.Data.Id)
            {
                Convertation.Data.LastContext = chat.Context;
                Convertation.Data.UpdatedDate = chat.InsertedDate;
                RenderActionChat(chat);
            }
            else
            {
                Task.Run(async () =>
                {
                    var con = await new Client(nameof(Convertation)).FirstOrDefaultAsync<Convertation>($"?$filter=Id eq {chat.ConvertationId}");
                    var chats = await new Client(nameof(Chat)).GetRawList<Chat>($"?$filter=ConvertationId eq {chat.ConvertationId}&$orderby=UpdatedDate desc");
                    var check = Convertations.Data.FirstOrDefault(x => x.Id == con.Id);
                    if (check != null)
                    {
                        Convertations.Data.FirstOrDefault(x => x.Id == con.Id).CopyPropFrom(con);
                    }
                    else
                    {
                        Convertations.Data.Add(con);
                    }
                    if (chats.Count > 0)
                    {
                        Chats.Data = chats;
                    }
                    Convertation.Data = con;
                    RenderChat();
                    RenderChats();
                    RenderUserChat();
                });
            }
        }

        private void Kick(object arg)
        {
            Task.Run(async () =>
            {
                var client = new Client(nameof(User));
                await client.CreateAsync<bool>(Client.Token, "SignOut");
                Client.Token = null;
                LocalStorage.RemoveItem("UserInfo");
                Window.Location.Reload(true);
            });
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
            var entity = Utils.GetEntity(task.EntityId ?? 0);
            task.Entity = new Entity { Id = entity.Id, Name = entity.Name };
            /*@
            if (typeof(Notification) !== 'undefined' && Notification.permission === "granted") {
                this.ShowNativeNtf(task);
            } else if (typeof(Notification) !== 'undefined' && Notification.permission !== "denied") {
                Notification.requestPermission().then((permission) => {
                    if (permission !== 'granted') {
                    }
                    else this.ShowNativeNtf(task);
                });
            }
            this.ShowToast(task);
            */
        }

        private int SetBadgeNumber()
        {
            var unreadCount = Notifications.Data.Count(x => x.StatusId == (int)TaskStateEnum.UnreadStatus);
            _countNtf.Data = unreadCount > 9 ? "9+" : unreadCount.ToString();
            _countUser.Data = UserActive.Data.Count.ToString();
            var badge = unreadCount > 9 ? 9 : unreadCount;
            /*@
            if (typeof(cordova) !== 'undefined' &&
                typeof(cordova.plugins) !== 'undefined' &&
                typeof(cordova.plugins.notification) !== 'undefined') {
                cordova.plugins.notification.badge.set(badge);
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
                    Toast.Success($"Thông báo hệ thống <br /> {task.Title} - {task.Description}");
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
            var convertations = new Client(nameof(Convertation)).GetRawList<Convertation>($"?$filter=FromId eq {Client.Token.UserId} or ToId eq {Client.Token.UserId}");
            await Task.WhenAll(notifications, userActive, convertations);
            Convertations.Data = convertations.Result;
            Notifications.Data = notifications.Result;
            UserActive.Data = userActive.Result;
            SetBadgeNumber();
            CurrentUser = Client.Token;
            CurrentUser.Avatar = (CurrentUser.Avatar.Contains("://") ? "" : Client.Origin) + (CurrentUser.Avatar.IsNullOrWhiteSpace() ? "./image/chinese.jfif" : CurrentUser.Avatar);
            RenderNotification();
            RenderUserActive();
            RenderUserChat();
            RenderChat();
            RenderProfile(".profile-info1");
        }

        private void RenderChat()
        {
            if (Convertation.Data != null)
            {
                Html.Take("#FullNameChat").InnerHTML(Convertation.Data.ToName);
            };
        }

        private void RenderChats()
        {
            Html.Take("#chat").Clear();
            if (Chats.Data != null)
            {
                Html.Take("#chat").ForEach(Chats.Data, (task, index) =>
                {
                    if (task.FromId == Client.Token.UserId)
                    {
                        Html.Instance.Div.ClassName("message parker").InnerHTML(task.Context).End.Render();
                    }
                    else
                    {
                        Html.Instance.Div.ClassName("message stark").InnerHTML(task.Context).End.Render();
                    }
                });
                var chat = Document.GetElementById("chat");
                chat.ScrollTop = chat.ScrollHeight - chat.ClientHeight;
            };
        }

        private void RenderActionChat(Chat chat)
        {
            if (chat.FromId == Client.Token.UserId)
            {
                Html.Take("#chat").Div.ClassName("message parker").InnerHTML(chat.Context).End.Render();
            }
            else
            {
                Html.Take("#chat").Div.ClassName("message stark").InnerHTML(chat.Context).End.Render();
            }
            var c = Document.GetElementById("chat");
            c.ScrollTop = c.ScrollHeight - c.ClientHeight;
        }

        public void RenderProfile(string classname)
        {
            var has = Document.QuerySelector("body").HasClass("theme-1");
            var isSave = Window.LocalStorage.GetItem("isSave");
            Html.Take(classname).Clear();
            var html = Html.Take(classname);
            html.A.ClassName("navbar-nav-link d-flex align-items-center dropdown-toggle").DataAttr("toggle", "dropdown").Span.ClassName("text-truncate").Text(CurrentUser.FullName).EndOf(ElementType.a)
                .Div.ClassName("dropdown-menu dropdown-menu-right notClose mt-0 border-0").Style("border-top-left-radius: 0;border-top-right-radius: 0")
                    .A.ClassName("dropdown-item").AsyncEvent(EventType.Click, ViewProfile).I.ClassName("far fa-user").End.Text("Account (" + CurrentUser.UserName + ")").EndOf(ElementType.a);
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            if (has)
            {
                html.A.ClassName("dropdown-item ui-mode").Event(EventType.Click, DarkMode).I.ClassName("fal fa-moon").End.Text("Dark mode").EndOf(ElementType.a);
            }
            else
            {
                html.A.ClassName("dropdown-item ui-mode").Event(EventType.Click, LightMode).I.ClassName("fal fa-adjust").End.Text("Light mode").EndOf(ElementType.a);
            }
            if (isSave is null)
            {
                html.A.ClassName("dropdown-item ui-mode").Event(EventType.Click, RemoveSetting).I.ClassName("fal fa-trash").End.Text("Đang lưu cài đặt").EndOf(ElementType.a);
            }
            else
            {
                html.A.ClassName("dropdown-item ui-mode").Event(EventType.Click, SaveSetting).I.ClassName("fal fa-save").End.Text("Đang không lưu cài đặt").EndOf(ElementType.a);
            }
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            var langSelect = new LangSelect(new Core.Models.Component(), html.GetContext());
            langSelect.Render();
            html.Div.ClassName("dropdown-divider").EndOf(ElementType.div);
            html.A.AsyncEvent(EventType.Click, SignOut).ClassName("dropdown-item").I.ClassName("far fa-power-off").End.Text("Logout").EndOf(ElementType.a);

            Html.Take(".btn-logout").AsyncEvent(EventType.Click, SignOut);
        }

        private void LightMode()
        {
            Document.QuerySelector("body").ReplaceClass("theme-2", "theme-1");
            RenderProfile(".profile-info1");
            LocalStorage.SetItem("theme", "theme-1");
            App.InitTheme();
        }

        private void DarkMode()
        {
            Document.QuerySelector("body").ReplaceClass("theme-1", "theme-2");
            RenderProfile(".profile-info1");
            LocalStorage.SetItem("theme", "theme-2");
            App.InitTheme();
        }

        private void RemoveSetting()
        {
            Window.LocalStorage.SetItem("isSave", true);
            RenderProfile(".profile-info1");
        }

        private void SaveSetting()
        {
            Window.LocalStorage.RemoveItem("isSave");
            RenderProfile(".profile-info1");
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
            Toast.Success("Logout success!");
            Client.Token = null;
            LocalStorage.RemoveItem("UserInfo");
            Window.Location.Reload();
        }

        private async Task ViewProfile(Event e)
        {
            var user = await new Client(nameof(User)).FirstOrDefaultAsync<User>($"?$filter=Active eq true and Id eq {CurrentUser.UserId}");
            await this.OpenPopup(featureName: "UserProfile",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.UserProfileBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "User Profile";
                    instance.Entity = user;
                    return instance;
                });
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

                var className = task.StatusId == (int)TaskStateEnum.UnreadStatus ? "text-danger" : "text-muted";
                html.A.ClassName("dropdown-item").Div.ClassName("media").Event(EventType.Click, async (e) =>
                {
                    await OpenNotification(task, e);
                })
                .Div.ClassName("media-body").H3.ClassName("dropdown-item-title").Text(task.Title).Span.ClassName("float-right text-sm " + className).I.ClassName("fas fa-star").End.End.End
                .P.ClassName("text-sm").Text(task.Description).End
                .P.ClassName("text-sm text-muted")
                    .I.ClassName("far fa-clock mr-1").End.Text(task.Deadline.ToString("dd/MM/yyyy HH:mm")).EndOf(ElementType.a);
            });
            html.A.ClassName("dropdown-item dropdown-footer").AsyncEvent(EventType.Click, SeeMore).Text("See more").EndOf(ElementType.a);
            Notifications.Data.ForEach(PopupNotification);
        }

        private void RenderUserActive()
        {
            var html = Html.Take("#user-active").A.ClassName("navbar-nav-link").DataAttr("toggle", "dropdown").I.ClassName("fal fa-users fa-lg").EndOf(ElementType.i);
            if (_countUser.Data != string.Empty)
            {
                html.Span.ClassName("badge badge-pill bg-warning-400 ml-auto ml-md-0").Text(_countUser);
                _countBadge = Html.Context;
            };
            html.EndOf(ElementType.a);
            html.Div.Style("border-top-left-radius: 0;border-top-right-radius: 0").ClassName("dropdown-menu dropdown-menu-right dropdown-content wmin-md-300 mt-0").Style("border-top-left-radius: 0;border-top-right-radius: 0");
            html.ForEach(UserActive, (task, index) =>
            {
                if (task is null)
                {
                    return;
                }
                html.A.ClassName("dropdown-item").Event(EventType.ContextMenu, UserActiveEdit, task).Div.ClassName("media")
                .Div.ClassName("media-body").H3.ClassName("dropdown-item-title").Text(task.FullName).Span.ClassName("float-right text-sm text-sucssess").I.ClassName("fas fa-star").End.End.End
                .P.ClassName("text-sm").Text("").End
                .P.ClassName("text-sm text-muted")
                    .I.ClassName("fal fa-tablet mr-1").End.Text(task.Recover).EndOf(ElementType.a);
            });
        }

        private void RenderUserChat()
        {
            if (UserActive.Data is null)
            {
                return;
            }
            Html.Take("#contacts").ForEach(UserActive.Data.DistinctBy(x => x.Id).ToList(), (task, index) =>
            {
                if (task is null)
                {
                    return;
                }
                Html.Instance.Div.ClassName("contact").AsyncEvent(EventType.Click, (e) => ChatByUser(e, task))
                .Div.ClassName("pic rogers").End
                .Div.ClassName("badge").End
                .Div.ClassName("name").Text(task.FullName).End
                .Div.ClassName("message").Text(task.Recover).End.End.Render();
            });
            Html.Take("#chats").ForEach(Convertations.Data.OrderByDescending(x => x.UpdatedDate).ToList(), (task, index) =>
            {
                if (task is null)
                {
                    return;
                }
                Html.Instance.Div.ClassName("contact").AsyncEvent(EventType.Click, (e) => ChatByConvertation(e, task))
                .Div.ClassName("pic rogers").End
                .Div.ClassName("badge").End
                .Div.ClassName("name").Text(task.FromId == Client.Token.UserId ? task.ToName : task.FromName).End
                .Div.ClassName("message").Text(task.LastContext).End.End.Render();
            });
            Html.Take("#input-chat").AsyncEvent(EventType.KeyDown, AddChat);
        }

        private async Task AddChat(Event e)
        {
            var keyCode = e.KeyCode();
            if (keyCode == (int)KeyCodeEnum.Enter)
            {
                var val = e.Target as HTMLInputElement;
                if (val.Value.IsNullOrWhiteSpace())
                {
                    return;
                }
                var chat = new Chat();
                if (Convertation.Data.ToId == Client.Token.UserId)
                {
                    chat = new Chat()
                    {
                        Context = val.Value,
                        ConvertationId = Convertation.Data.Id,
                        FromId = Convertation.Data.ToId,
                        ToId = Convertation.Data.FromId,
                        IsSeft = true,
                    };
                }
                else
                {
                    chat = new Chat()
                    {
                        Context = val.Value,
                        ConvertationId = Convertation.Data.Id,
                        FromId = Convertation.Data.FromId,
                        ToId = Convertation.Data.ToId,
                        IsSeft = true,
                    };
                }
                val.Value = string.Empty;
                Chats.Data.Add(chat);
                RenderActionChat(chat);
                var loader = Document.GetElementsByClassName("lds-ellipsis").FirstOrDefault();
                loader.Style.Display = "block";
                await new Client(nameof(Chat)).CreateAsync<Chat>(chat);
                loader.Style.Display = "none";
            }
        }

        private async Task ChatByUser(Event e, User user)
        {
            var con = await new Client(nameof(Convertation)).FirstOrDefaultAsync<Convertation>($"?$filter=(ToId eq {user.Id} and FromId eq {Client.Token.UserId}) or (FromId eq {user.Id} and ToId eq {Client.Token.UserId})");
            if (con is null)
            {
                con = new Convertation()
                {
                    FromId = Client.Token.UserId,
                    ToId = user.Id,
                    FromName = Client.Token.FullName,
                    ToName = user.FullName,
                };
                con = await new Client(nameof(Convertation)).CreateAsync<Convertation>(con);
            }
            else
            {
                var chats = await new Client(nameof(Chat)).GetRawList<Chat>($"?$filter=ConvertationId eq {con.Id}");
                Chats.Data = chats;
            }
            Convertation.Data = con;
            RenderChat();
            RenderChats();
        }

        private async Task ChatByConvertation(Event e, Convertation convertation)
        {
            var chats = await new Client(nameof(Chat)).GetRawList<Chat>($"?$filter=ConvertationId eq {convertation.Id}");
            Chats.Data = chats;
            Convertation.Data = convertation;
            RenderChat();
            RenderChats();
        }

        private void UserActiveEdit(Event e, User user)
        {
            if (!Client.SystemRole)
            {
                return;
            }
            var menuItems = new List<ContextMenuItem>()
            {
                new ContextMenuItem { Icon = "far fa-sign-out mt-2", Text = "Đăng xuất", Click = LogOut, Parameter = user },
                new ContextMenuItem { Icon = "fa fa-undo mt-2", Text = "Reload", Click = LogOut, Parameter = user },
                new ContextMenuItem { Icon = "far fa-envelope mt-2", Text = "Nhắn tin", Click = LogOut, Parameter = user },
                new ContextMenuItem { Icon = "far fa-bell mt-2", Text = "Thông báo cập nhật", Click = LogOut, Parameter = user },
            };
            e.PreventDefault();
            e.StopPropagation();
            var ctxMenu = ContextMenu.Instance;
            ctxMenu.Top = e.Top();
            ctxMenu.Left = e.Left();
            ctxMenu.MenuItems = menuItems;
            ctxMenu.Render();
        }

        private void LogOut(object e)
        {
            var task = e as User;
            Task.Run(async () =>
            {
                await new Client(nameof(TaskNotification)).PostAsync<bool>(task.Email, "KickOut");
            });
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
            var res = await client.PostAsync<bool>(client, "MarkAllAsRead");
            ToggleBageCount(Notifications.Data.Count);
            _task.QuerySelectorAll(".text-danger").ForEach(task =>
            {
                task.ReplaceClass("text-danger", "text-muted");
            });
        }

        public async Task OpenNotification(TaskNotification notification, Event e)
        {
            await MarkAsRead(notification);
            var element = (e.Target as HTMLElement).Closest("a");
            var span = element.QuerySelector(".text-danger");
            if (span != null)
            {
                element.QuerySelector(".text-danger").ReplaceClass("text-danger", "text-muted");
            }
            if (notification.EntityId == Utils.GetEntity(nameof(TransportationPlan)).Id)
            {
                var entity = await new Client(nameof(TransportationPlan)).GetRawAsync(notification.RecordId.Value);
                await this.OpenPopup(
                featureName: "TransportationPlan Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa kế hoạch vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
            }
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
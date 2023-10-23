using Bridge.Html5;
using Core.Components;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.User
{
    public class UserBL : TabEditor
    {
        public UserBL() : base(nameof(API.Models.User))
        {
            Name = "User List";
            Title = Name;
        }

        public void EditUser(API.Models.User user)
        {
            InitUserForm(user);
        }

        public void CreateUser()
        {
            InitUserForm(new API.Models.User());
        }

        public void CreateTaskNotification()
        {
            var a = this.OpenPopup(
                featureName: "Create TaskNotification",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.TaskNotificationDetailBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới thông báo";
                    instance.Entity = new TaskNotification();
                    return instance;
                });
            var promise = ClientExt.ToPromise(a);
            /*@
            promise.then(x => console.log('ok'));
             */
        }

        private void InitUserForm(API.Models.User user)
        {
            var a = this.OpenPopup(
                featureName: "User Detail",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.UserDetailBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    //instance.Title = user.Id <= 0 ? "Add user" : "Edit user";
                    instance.Entity = user;
                    return instance;
                });
            var promise = ClientExt.ToPromise(a);
            /*@
            promise.then(x => console.log('ok'));
             */
        }
    }
}

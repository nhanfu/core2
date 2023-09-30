using Core.ViewModels;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;
using TMS.UI.Notifications;

namespace TMS.UI.Business.User
{
    public class UserProfileBL : PopupEditor
    {
        public API.Models.User UserEntity => Entity as API.Models.User;
        public UserProfileBL() : base(nameof(API.Models.User))
        {
            Name = "UserProfile";
            DOMContentLoaded += () =>
            {
                CheckShowNative();
            };
        }

        public async Task OpenSetting()
        {
            await this.OpenPopup(featureName: "UserSetting",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.UserSettingBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "User Setting";
                    return instance;
                });
        }

        public async Task OpenChangePassword()
        {
            await this.OpenPopup(featureName: "ChangePassword",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.User.ChangePasswordBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Entity = new UserProfileVM()
                    {
                        //Id = UserEntity.Id
                    };
                    instance.Title = "Change Password";
                    return instance;
                });
        }

        public void ShowButtonNative()
        {
            this.FindComponentByName<Button>("btnNative").Show = false;
            this.FindComponentByName<Section>("Image").UpdateView();
        }

        public void ShowNative()
        {
            /*@Notification.requestPermission()
             */
        }

        public void CheckShowNative()
        {
            /*@
             * if(Notification.permission === "granted")
             *      this.ShowButtonNative()
             * else if(Notification.permission === "denied")
             */
            Toast.Warning("Vui lòng gỡ block thông báo cho trang này!");
        }
    }
}

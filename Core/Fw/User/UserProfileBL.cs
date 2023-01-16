using Core.ViewModels;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Threading.Tasks;
using Core.Notifications;

namespace Core.Fw.User
{
    public class UserProfileBL : TabEditor
    {
        public UserProfileBL() : base(nameof(Core.Models.User))
        {
            var user = new UserProfileVM
            {
                Id = Client.Token.UserId
            };
            ShouldLoadEntity = true;
            Entity = user;
            Name = "UserProfile";
            Title = "User profile";
            Icon = "/icons/profile.png";
            Client = new Client(nameof(Models.User));
            DOMContentLoaded += () =>
            {
                CheckShowNative();
            };
        }

        public async Task OpenChangePassword()
        {
            var vm = Entity as UserProfileVM;
            await this.OpenPopup(featureName: "ChangePassword",
                factory: () =>
                {
                    var type = Type.GetType("Core.Fw.User.ChangePasswordBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Entity = vm;
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

        public override async Task<bool> Save(object entity)
        {
            if (!Dirty)
            {
                Toast.Warning(NotDirtyMessage);
                return false;
            }
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return false;
            }
            var vm = Entity as UserProfileVM;
            vm.ClearReferences();
            vm.OldPassword = null;
            vm.NewPassword = null;
            vm.ConfirmedPassword = null;
            var saved = await Client.UpdateAsync<bool>(vm, "UpdateProfile");
            if (saved)
            {
                Toast.Success("Update profile succeeded!");
                await NotificationBL.Instance.RenderAsync();
            }
            else
            {
                Toast.Warning("Update profile failed!");
            }
            Dirty = false;
            return saved;
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

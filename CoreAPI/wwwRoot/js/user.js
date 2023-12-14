let methods = {
    ReSend: function () {
        var sql = {
            ComId: 'User', Action: 'ResendPass', Ids: [this.UserEntity.Id]
        };
        var xhr = {
            IsRawString: true, Value: JSON.stringify(sql), Url: '/user/svc/', Method: 1
        };
        var task = Core.Clients.Client.Instance.SubmitAsync(System.String, xhr);
        var promise = Core.Extensions.EventExt.ToPromise(task);
        promise.then(pass => {
            var dialog = new Core.Components.Forms.ConfirmDialog();
            dialog.Title = `Đổi mật khẩu cho user ${this.UserEntity.UserName}`;
            dialog.Content = `Đổi mật khẩu cho user thành công.<br /> Mật khẩu mới là ${pass}}`;
            dialog.IgnoreNoButton = true;
            dialog.Render();
        });
    },
    Check_User: function (userRole, role) {
        this.gridView = this.gridView || System.Linq.Enumerable.from(Core.Components.Extensions.ComponentExt.FindActiveComponent(Core.Components.GridView, this), Core.Components.GridView).firstOrDefault(null, null);
        if (System.Linq.Enumerable.from(this.UserEntity.UserRole, TMS.API.Models.UserRole).any(function (x) {
            return !Bridge.referenceEquals(x.Id, userRole.Id) && Bridge.referenceEquals(x.RoleId, role.Id);
        })) {
            Core.Extensions.Toast.Warning("T\u00e0i kho\u1ea3n n\u00e0y \u0111\u00e3 t\u1ed3n t\u1ea1i !!!");
            this.gridView.RemoveRow(userRole);
        }
    }
}

return methods;
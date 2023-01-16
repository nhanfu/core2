using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class BankAccountListBL : TabEditor
    {
        public BankAccountListBL() : base(nameof(BankAccount))
        {
            Name = "BankAccount List";
        }

        public async Task AddBankAccount()
        {
            await this.OpenPopup(
                featureName: "BankAccount Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.BankAccountEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới tài khoản ngân hàng";
                    instance.Entity = new BankAccount();
                    return instance;
                });
        }

        public async Task EditBankAccount(BankAccount bankAccount)
        {
            await this.OpenPopup(
                featureName: "BankAccount Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.BankAccountEditBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa tài khoản ngân hàng";
                    instance.Entity = bankAccount;
                    return instance;
                });
        }
    }
}

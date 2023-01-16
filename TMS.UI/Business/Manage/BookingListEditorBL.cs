using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class BookingListEditorBL : PopupEditor
    {
        public BookingList BookingListEntity => Entity as BookingList;
        public BookingListEditorBL() : base(nameof(BookingList))
        {
            Name = "Ship Book Editor";
        }

        public async Task CreateBookingList()
        {
            if (BookingListEntity.FromDate is null)
            {
                Toast.Warning("Vui lòng nhập từ ngày");
                return;
            }
            if (BookingListEntity.ToDate is null)
            {
                Toast.Warning("Vui lòng nhập đến ngày");
                return;
            }
            await new Client(nameof(BookingList)).PostAsync<bool>(Entity, "UpdateBookingList");
            Dirty = false;
            this.Dispose();
        }

        public override void Cancel()
        {
            this.Dispose();
            base.Cancel();
        }

        public override void CancelWithoutAsk()
        {
            this.Dispose();
            base.CancelWithoutAsk();
        }
    }
}

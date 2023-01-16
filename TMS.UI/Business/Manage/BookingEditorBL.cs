using Core.Components.Forms;
using Core.Extensions;
using System;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class BookingEditorBL : PopupEditor
    {
        public Booking booking => Entity as Booking;
        public BookingEditorBL() : base(nameof(Booking))
        {
            Name = "Booking Editor";
        }

        public void CalcTeusEditor()
        {
            if (booking.Teus20Using > booking.Teus20)
            {
                Toast.Warning("Số teus20 đóng không được lớn hơn số teus cấp");
                booking.Teus20Using = booking.Teus20;
            }
            if (Convert.ToDecimal(booking.Teus40Using) > Convert.ToDecimal(booking.Teus40))
            {
                Toast.Warning("Số teus40 đóng không được lớn hơn số teus cấp");
                booking.Teus40Using = booking.Teus40;
            }
            booking.Teus20Remain = booking.Teus20 - booking.Teus20Using;
            booking.Teus40Remain = booking.Teus40 - booking.Teus40Using;
            UpdateView();
        }
    }
}
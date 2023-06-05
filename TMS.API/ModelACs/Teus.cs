using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Teus
    {
        public int Id { get; set; }
        public int? BrandShipId { get; set; }
        public int? ShipId { get; set; }
        public string Trip { get; set; }
        public DateTime? StartShip { get; set; }
        public decimal Teus20 { get; set; }
        public decimal Teus40 { get; set; }
        public decimal Teus20Using { get; set; }
        public decimal Teus40Using { get; set; }
        public decimal Teus20Remain { get; set; }
        public decimal Teus40Remain { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string Note { get; set; }
        public string Note1 { get; set; }
        public string Note2 { get; set; }
        public string Note3 { get; set; }
        public string Note4 { get; set; }
        public int? BranchId { get; set; }
    }
}

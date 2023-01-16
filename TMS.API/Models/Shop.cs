using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Shop
    {
        public Shop()
        {
            ErrorLog = new HashSet<ErrorLog>();
            User = new HashSet<User>();
        }

        public int Id { get; set; }
        public int? Name { get; set; }
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public int? TotalFollow { get; set; }
        public string Logo { get; set; }
        public int? TypeId { get; set; }
        public int? TotalStar { get; set; }
        public int? TotalCountStar { get; set; }
        public int? TotalProduct { get; set; }
        public decimal ReturnRate { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual ICollection<ErrorLog> ErrorLog { get; set; }
        public virtual ICollection<User> User { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class UserLogin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }
        public DateTime? SignInDate { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpiredDate { get; set; }
    }
}

using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class Admin
    {
        public long IAdminId { get; set; }
        public string UserName { get; set; }
        public string MobileNumber { get; set; }
        public long? Otp { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}

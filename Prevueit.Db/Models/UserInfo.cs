using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class UserInfo
    {
        public long IUserInfoId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserEmail { get; set; }
        public string TempName { get; set; }
        public long? PhoneNumber { get; set; }
        public int? Otp { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Token { get; set; }
        public bool? IsProfileChanged { get; set; }
        public string ProfilePicUrl { get; set; }
        public bool? IsPaidUser { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? IsPermanentyBlock { get; set; }
        public string Remarks { get; set; }
        public DateTime? BlockDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Model
{
    public class UserModel
    {
        public int IUserInfoId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserEmail { get; set; }
        public string TempName { get; set; }
        public long? PhoneNumber { get; set; }
        public int? Otp { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public string ProfilePicURL { get; set; }
        public bool IsProfileChanged { get; set; }
        public bool IsPaidUser { get; set; }
        public string TotalSpaceUsed { get; set; }
        public string UserType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Remarks { get; set; }
    }

    public class UserLogin
    {
        public string UserEmail { get; set; }
        public string Token { get; set; }

    }

    public class SignUpReq
    {
        public string UserEmail { get; set; }
    }

    public class NotificationModel
    {
        public int INotificationId { get; set; }
        public int IUserId { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProfilePicUrl { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class BlockUserReqModel
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string Remarks { get; set; }
        public bool IsPermanentlyBlock { get; set; }
    }

}

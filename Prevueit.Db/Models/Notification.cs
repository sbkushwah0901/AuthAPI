using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class Notification
    {
        public long INotificationId { get; set; }
        public long? IUserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? CreateDate { get; set; }
        public bool? IsActive { get; set; }
        public string FromUserName { get; set; }
        public int? FromUserId { get; set; }
    }
}

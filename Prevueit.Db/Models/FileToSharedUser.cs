using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class FileToSharedUser
    {
        public long IFileToSharedUserId { get; set; }
        public long? IFileId { get; set; }
        public string UserEmail { get; set; }
        public DateTime? UploadedDate { get; set; }
        public string FromUserEmail { get; set; }
    }
}

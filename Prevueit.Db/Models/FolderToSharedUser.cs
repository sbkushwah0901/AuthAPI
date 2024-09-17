using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class FolderToSharedUser
    {
        public long IFolderToSharedUserId { get; set; }
        public long? IFolderId { get; set; }
        public string ToUserEmail { get; set; }
        public DateTime? UploadedDate { get; set; }
        public string FromUserEmail { get; set; }
    }
}

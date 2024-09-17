using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class Folder
    {
        public long IFolderId { get; set; }
        public string FolderName { get; set; }
        public long? IUserId { get; set; }
        public DateTime? FolderCreateDate { get; set; }
        public bool? IsFavourite { get; set; }
    }
}

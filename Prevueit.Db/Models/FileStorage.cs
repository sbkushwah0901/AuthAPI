using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class FileStorage
    {
        public long IFileId { get; set; }
        public string FileOriginalName { get; set; }
        public string FileUrl { get; set; }
        public int? IUserInfoId { get; set; }
        public string SharedEmail { get; set; }
        public string Description { get; set; }
        public string FileSize { get; set; }
        public string SharebleLink { get; set; }
        public string LongDescription { get; set; }
        public int? ViewedCount { get; set; }
        public int? UploadedBy { get; set; }
        public bool? IsReviewed { get; set; }
        public DateTime? UploadedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string FileType { get; set; }
        public string ContentType { get; set; }
        public string AzureFileName { get; set; }
        public bool? IsFavourite { get; set; }
        public long? IFolderId { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string ReviewedByEmail { get; set; }
        public DateTime? DuplicateDate { get; set; }
        public string DuplicateByEmail { get; set; }
    }
}

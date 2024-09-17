using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class Comment
    {
        public long ICommentId { get; set; }
        public long? IFileId { get; set; }
        public long? IUserId { get; set; }
        public string CommentText { get; set; }
        public string CommentType { get; set; }
        public string CommentUrl { get; set; }
        public long? IParentCommentId { get; set; }
        public string VideoFrametime { get; set; }
        public int? LikeCount { get; set; }
        public DateTime? CommentCreateDate { get; set; }
        public DateTime? CommentUpdateDate { get; set; }
    }
}

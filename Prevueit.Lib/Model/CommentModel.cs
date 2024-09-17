using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Model
{
    public class CommentModel
    {
        public int ICommentId { get; set; }
        public int IFileId { get; set; }
        public int IUserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CommentText { get; set; }
        public string CommentType { get; set; }
        public string CommentUrl { get; set; }
        public int IParentCommentId { get; set; }
        public string VideoFrametime { get; set; }
        public int LikeCount { get; set; }
        public DateTime CommentCreateDate { get; set; }
        public DateTime CommentUpdateDate { get; set; }
    }
}

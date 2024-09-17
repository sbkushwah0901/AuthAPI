using Prevueit.Db.Models;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Prevueit.Lib.Interface
{
    public interface ICommentLibrary
    {
        Task<ResponseModel<string>> UploadComment(dynamic files);
        ResponseModel<bool> AddUpdateComment(Comment objComment);
        Task<ResponseModel<bool>> RemoveComment(int iCommentId);
        ResponseModel<List<CommentModel>> GetCommentsByFileId(int iFileId);
        ResponseModel<int> LikeComment(int iCommentId);
    }
}

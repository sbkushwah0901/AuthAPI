using Microsoft.AspNetCore.Http;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prevueit.Lib.Interface
{
    public interface IFileLibrary
    {
        ResponseModel<bool> UploadFiles(FileUploadReqModel lstFile);
        Task<ResponseModel<string>> Upload(dynamic files);
        ResponseModel<bool> UpdateFileDetail(FileUploadReqModel fileModel);
        ResponseModel<FileModel> GetFileDetailsId(int iFileId);
        ResponseModel<bool> UpdateReviewedStatus(int iFileId, string userEmail);
        ResponseModel<List<FileModel>> GetFileSharedUserId(string userEmail, string fileType);
        ResponseModel<List<FileModel>> GetFilesByUploadedUserId(int iUserId, string fileType, int iFolderId);
        ResponseModel<List<FileModel>> GetSentFilesByUserId(int iUserId);
        Task<ResponseModel<bool>> DeleteFile(int iFileId);
        ResponseModel<bool> AddSharedUser(AddUserToFileModel reqModel);
        ResponseModel<bool> RemoveSharedUser(int iFileId, int iFolderId, string userEmails);
        ResponseModel<bool> IncrementViewCount(int iFileId);
        ResponseModel<bool> SendEmail(EmailReqModel reqModel);
        ResponseModel<bool> AddRemoveFavourites(int iFileFolderId, bool isFavourite, bool isFolder);
        ResponseModel<bool> RenameFileFolder(int iFileFolderId, string newName, bool isFolder);
        ResponseModel<int> CreateFolder(FolderDetail reqModel);
        ResponseModel<bool> MoveToFolder(int iFolderId, int iFileId);
        ResponseModel<List<FolderDetail>> GetFoldersByUserId(int iUserInfoId);
        Task<ResponseModel<bool>> DeleteFolder(int iFolderId);
        ResponseModel<FolderWithFileModel> FetFolderDetailWithFiles(int iFolderId);
        Task<ResponseModel<bool>> DuplicateFile(int iFileId, int iUserId);
        ResponseModel<List<SearchFiles>> GetSearchFileLists(string fileType, int iUserId, string userEmail);
    }
}

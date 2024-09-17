using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Model
{
    public class FileModel
    {
        public long IFileId { get; set; }
        public string FileOriginalName { get; set; }
        public string AzureFileName { get; set; }
        public string FileUrl { get; set; }
        public int IUserInfoId { get; set; }
        public List<string> SharedEmail { get; set; }
        public List<FileUserDetail> FileUserDetails { get; set; }
        public string StrSharedEmail { get; set; }
        public string Description { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string ContentType { get; set; }
        public string SharebleLink { get; set; }
        public string LongDescription { get; set; }
        public int ViewedCount { get; set; }
        public bool isReviewed { get; set; }
        public string OwnerEmail { get; set; }
        public bool isFavourite { get; set; }
        public int UploadedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? UploadedDate { get; set; }
        public int iFolderId { get; set; }
        public string FolderName { get; set; }
        public string FolderSize { get; set; }
        public int FilesCount { get; set; }
        public DateTime? FolderCreateDate { get; set; }
        public bool IsFolder { get; set; }
        public DateTime? FolderFileCreateDate { get; set; }
        public FileActivity fileActivity { get; set; }
        public FolderActivity folderActivity { get; set; }
    }
    public class FileUploadReqModel
    {
        public long IFileId { get; set; }
        public string FileOriginalName { get; set; }
        public string FileUrl { get; set; }
        public int IUserInfoId { get; set; }
        public string SharedEmail { get; set; }
        public string Description { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string ContentType { get; set; }
        public string SharebleLink { get; set; }
        public string LongDescription { get; set; }
        public int ViewedCount { get; set; }
        public int UploadedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string AzureFileName { get; set; }
        public int iFolderId { get; set; }
    }

    public class FileSizePerUser
    {
        public string TotalBytes { get; set; }
        public string UsedBytes { get; set; }
        public string RemainingBytes { get; set; }
    }

    public class AddUserToFileModel
    {
        public int FileId { get; set; }
        public int iFolderId { get; set; }
        public string ToUserEmails { get; set; }
        public string FromUserEmail { get; set; }
    }

    public class FileUserDetail
    {
        public string OwnerEmail { get; set; }
        public string SendFromEmail { get; set; }
        public string SendToEmail { get; set; }
        public string SendToUserName { get; set; }
        public string SendToUserProfilePic { get; set; }
        public string OwnerUserName { get; set; }
        public string OwnerUserProfilePic { get; set; }

    }
    public class FolderDetail
    {
        public int iFolderId { get; set; }
        public string FolderName { get; set; }
        public int iUserInfoId   { get; set; }
    }
    public class FolderWithFileModel
    {
        public int iFolderId { get; set;}
        public string FolderName { get; set; }
        public string FolderSize { get; set; }
        public int FileCount { get; set; }
        public List<FileModel> Files { get; set; }
    }
    public class EmailReqModel
    {
        public string EnumEmailFor { get; set; }
        public string ToEmail { get; set; }
        public int ToUserId { get; set; }
        public string FromUserName { get; set; }
        public int FromUserId { get; set; }
        public string Token { get; set; }
       
    }
    public class FileActivity
    {
        public DateTime FileUploadedDate { get; set; }
        public DateTime? FileReviewedDate { get; set; }
        public DateTime? FileDuplicateDate { get; set; }
        public string FileUploadedBy { get; set; }
        public string FileReviewedBy { get; set; }
        public string FileDuplicatedBy { get; set; }

    }
    public class FolderActivity
    {
        public DateTime FolderCreatedDate { get; set; }
        public string FolderCreatedBy { get; set; }
        public List<string> FolderFileNames { get; set; }

    }
    public class SearchFiles
    {
        public string FileOriginalName { get; set; }
        public int iFileId { get; set; }
        public int iFolderId { get; set; }
        public string FolderName { get; set; }
        public DateTime FileUploadedDate { get; set; }

    }
}

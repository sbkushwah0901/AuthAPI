using Prevueit.Db.Models;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Net.Mail;
using static Prevueit.Lib.Enum;
using System.Globalization;

namespace Prevueit.Lib.Implementation
{
    public class FileLibrary : IFileLibrary
    {
        #region Variable Declartion
        private readonly prevuitContext _dbContext;
        private readonly AppSettingsModel m_appSettingsModel;
        private readonly ICommonLibrary _commonLibrary;
        #endregion

        #region constructor
        public FileLibrary(prevuitContext dbContext, AppSettingsModel appSettingsModel, ICommonLibrary commonLibrary)
        {
            _dbContext = dbContext;
            m_appSettingsModel = appSettingsModel;
            _commonLibrary = commonLibrary;
        }
        #endregion

        public ResponseModel<bool> UploadFiles(FileUploadReqModel fileModel)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = new FileStorage()
                {
                    FileOriginalName = fileModel.FileOriginalName,
                    FileUrl = fileModel.FileUrl,
                    IUserInfoId = fileModel.IUserInfoId,
                    SharedEmail = fileModel.SharedEmail,
                    Description = fileModel.Description,
                    FileSize = fileModel.FileSize,
                    SharebleLink = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    LongDescription = fileModel.LongDescription,
                    ViewedCount = 0,
                    UploadedBy = fileModel.UploadedBy,
                    UploadedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddMonths(1),
                    FileType = fileModel.FileType,
                    ContentType = fileModel.ContentType,
                    AzureFileName = fileModel.AzureFileName != null ? fileModel.AzureFileName : "",
                    IFolderId = fileModel.iFolderId
                };

                _dbContext.FileStorage.Add(objFile);
                _dbContext.SaveChanges();

                var iFileId = objFile.IFileId;
                if (fileModel.SharedEmail != null)
                {
                    var objEmails = fileModel.SharedEmail.ToString().Split(',');
                    if (objEmails.Length > 0)
                    {
                        foreach (var email in objEmails)
                        {
                            var fileToUser = new FileToSharedUser()
                            {
                                IFileId = iFileId,
                                UserEmail = email,
                                UploadedDate = DateTime.Now
                            };
                            _dbContext.FileToSharedUser.Add(fileToUser);
                            _dbContext.SaveChanges();
                            var existUser = _dbContext.UserInfo.Where(x => x.UserEmail == email).FirstOrDefault();
                            if (existUser != null)
                            {
                                sendEmail(existUser, EnumEmailType.fileShare, fileModel.IUserInfoId, objFile.FileOriginalName, FormatSize(objFile.FileSize), FormatDate(Convert.ToDateTime(objFile.ExpiryDate)));
                            }
                            else
                            {
                                UserInfo user = new UserInfo()
                                {
                                    FirstName = "",
                                    LastName = "",
                                    UserEmail = email,
                                    TempName = "Guest User",
                                    CreatedDate = DateTime.Now
                                };
                                _dbContext.UserInfo.Add(user);
                                _dbContext.SaveChanges();

                                var token = generateToken(user.UserEmail, Convert.ToInt16(user.IUserInfoId));
                                user.Token = token;
                                _dbContext.UserInfo.Update(user);
                                _dbContext.SaveChanges();
                                _commonLibrary.sendMail(user.UserEmail, EnumEmailType.registerUser, token, user.TempName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                                sendEmail(user, EnumEmailType.fileShare, fileModel.IUserInfoId, objFile.FileOriginalName, FormatSize(objFile.FileSize), FormatDate(Convert.ToDateTime(objFile.ExpiryDate)));
                            }
                        }
                    }
                }


                ////check storage for user
                //long usedByes = 0;
                //long gb = 1024 * 1024 * 1024;
                //long remainingBytes = 5 * gb;
                //long limitBytes = 4 * gb;
                //var files = _dbContext.FileStorage.Where(x => x.IUserInfoId == fileModel.IUserInfoId).ToList();
                //if (files != null)
                //{
                //    foreach (var file in files)
                //    {
                //        usedByes = Convert.ToInt64(usedByes + Convert.ToInt64(file.FileSize));
                //    }
                //}
                //remainingBytes = remainingBytes - usedByes;
                //if (remainingBytes <= limitBytes)
                //{
                //    var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == fileModel.IUserInfoId).FirstOrDefault();
                //    string userName = user != null ? user.FirstName != null ? user.FirstName + " " + user.LastName : user.TempName : "";
                //    _commonLibrary.sendMail(user.UserEmail, EnumEmailType.storageLimit, "", userName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                //}

                res.IsSuccess = true;
                res.Message = "File uploaded successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public async Task<ResponseModel<string>> Upload(dynamic files)
        {
            ResponseModel<string> res = new ResponseModel<string>();
            try
            {
                string path = "";
                string originalName = "";
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                            originalName = Convert.ToString(fileName);
                            var objName = Convert.ToString(fileName).Split(".");
                            fileName = "File" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + objName[1];
                            string storageAccountConnectionString = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                            string containerName = m_appSettingsModel.AzureStorage.ContainerName.ToString();
                            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
                            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);
                            await Container.CreateIfNotExistsAsync();
                            CloudBlockBlob blob = Container.GetBlockBlobReference(fileName);
                            HashSet<string> blocklist = new HashSet<string>();
                            path = containerName + "/" + fileName;
                            const int pageSizeInBytes = 104857600;
                            long prevLastByte = 0;
                            long bytesRemain = file.Length;

                            byte[] bytes;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var fileStream = file.OpenReadStream();
                                await fileStream.CopyToAsync(ms);
                                bytes = ms.ToArray();
                            }
                            var contentType = "";
                            if (Convert.ToString(file.ContentType).Contains("audio"))
                            {
                                contentType = "video/mp4";
                            }
                            else
                            {
                                contentType = Convert.ToString(file.ContentType);
                            }
                            // Upload each piece
                            do
                            {
                                long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
                                byte[] bytesToSend = new byte[bytesToCopy];

                                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
                                prevLastByte += bytesToCopy;
                                bytesRemain -= bytesToCopy;

                                //create blockId
                                string blockId = Guid.NewGuid().ToString();
                                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                                blob.Properties.ContentType = contentType;
                                await blob.PutBlockAsync(
                                    base64BlockId,
                                    new MemoryStream(bytesToSend, true),
                                    null
                                    );

                                blocklist.Add(base64BlockId);

                            } while (bytesRemain > 0);

                            //post blocklist
                            await blob.PutBlockListAsync(blocklist);

                        }
                    }

                }

                res.ResponseData = m_appSettingsModel.AzureStorage.AzureFileURL.ToString() + path;
                res.IsSuccess = true;
                res.Message = originalName;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = "";
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<bool> UpdateFileDetail(FileUploadReqModel fileModel)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == fileModel.IFileId).FirstOrDefault();
                if (objFile != null)
                {
                    objFile.Description = fileModel.Description;
                    objFile.LongDescription = fileModel.LongDescription;
                    objFile.ExpiryDate = fileModel.ExpiryDate;

                    _dbContext.FileStorage.Update(objFile);
                    _dbContext.SaveChanges();
                }

                res.Message = "File detail updated successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<FileModel> GetFileDetailsId(int iFileId)
        {
            ResponseModel<FileModel> res = new ResponseModel<FileModel>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileId).FirstOrDefault();
                var ownerUser = _dbContext.UserInfo.Where(x => x.IUserInfoId == objFile.IUserInfoId).FirstOrDefault();
                if (objFile != null)
                {
                    var users = _dbContext.FileToSharedUser.Where(x => x.IFileId == iFileId).ToList();
                    List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();
                    foreach (var user in users)
                    {
                        var sendToEmail = user.UserEmail == null ? ownerUser.UserEmail : user.UserEmail;
                        var userDetail = _dbContext.UserInfo.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                        fileUserDetails.Add(new FileUserDetail()
                        {
                            OwnerEmail = ownerUser.UserEmail,
                            SendToEmail = sendToEmail,
                            SendFromEmail = user.FromUserEmail,
                            SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                            SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                            OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                            OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                        });
                    }   
                    var file = new FileModel()
                    {
                        IFileId = objFile.IFileId,
                        FileOriginalName = objFile.FileOriginalName,
                        AzureFileName = objFile.AzureFileName,
                        FileUrl = objFile.FileUrl,
                        IUserInfoId = Convert.ToInt16(objFile.IUserInfoId),
                        SharedEmail = users.Select(x => x.UserEmail).ToList(),
                        StrSharedEmail = objFile.SharedEmail,
                        Description = objFile.Description,
                        FileSize = objFile.FileSize,
                        FileType = objFile.FileType,
                        SharebleLink = objFile.SharebleLink,
                        LongDescription = objFile.LongDescription,
                        ViewedCount = Convert.ToInt16(objFile.ViewedCount),
                        isReviewed = Convert.ToBoolean(objFile.IsReviewed),
                        UploadedBy = Convert.ToInt16(objFile.UploadedBy),
                        ExpiryDate = Convert.ToDateTime(objFile.ExpiryDate),
                        UploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                        ContentType = objFile.ContentType,
                        FileUserDetails = fileUserDetails
                    };
                    res.ResponseData = file;
                }
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public ResponseModel<bool> UpdateReviewedStatus(int iFileId, string userEmail)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileId).FirstOrDefault();
                if (objFile != null)
                {
                    objFile.IsReviewed = true;
                    objFile.ReviewedByEmail = userEmail;
                    objFile.ReviewedDate = DateTime.Now;
                    _dbContext.FileStorage.Update(objFile);
                    _dbContext.SaveChanges();
                }

                res.Message = "File reviewed successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<List<FileModel>> GetFileSharedUserId(string userEmail, string fileType)
        {
            ResponseModel<List<FileModel>> res = new ResponseModel<List<FileModel>>();
            try
            {
                var lstFiles = new List<FileModel>();
                var SharedUser = _dbContext.FileToSharedUser.Where(x => x.UserEmail == userEmail).ToList();
                var userList = _dbContext.UserInfo.ToList();
                if (SharedUser != null && SharedUser.Count > 0)
                {
                    foreach (var item in SharedUser)
                    {
                        FileStorage objFile = new FileStorage(); 

                        if (fileType == "0")
                        {
                            objFile = _dbContext.FileStorage.Where(x => x.IFileId == item.IFileId).FirstOrDefault();
                        }
                        else if (fileType == "favourites")
                        {
                            objFile = _dbContext.FileStorage.Where(x => x.IFileId == item.IFileId && x.IsFavourite == true).FirstOrDefault();
                        }
                        else
                        {
                            objFile = _dbContext.FileStorage.Where(x => x.IFileId == item.IFileId && x.FileType == fileType).FirstOrDefault();
                        }

                        if (objFile != null)
                        {
                            var ownerUser = userList.Where(x => x.IUserInfoId == objFile.IUserInfoId).FirstOrDefault();
                            List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();

                            var sendToEmail = item.UserEmail == null ? ownerUser.UserEmail : item.UserEmail;
                            var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                            fileUserDetails.Add(new FileUserDetail()
                            {
                                OwnerEmail = ownerUser.UserEmail,
                                SendToEmail = sendToEmail,
                                SendFromEmail = item.FromUserEmail,
                                SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                            });

                            var obj = new FileModel()
                            {
                                IFileId = objFile.IFileId,
                                FileOriginalName = objFile.FileOriginalName,
                                AzureFileName = objFile.AzureFileName,
                                FileUrl = objFile.FileUrl,
                                IUserInfoId = Convert.ToInt16(objFile.IUserInfoId),
                                SharedEmail = objFile.SharedEmail != null ? objFile.SharedEmail.Split(',').ToList() : null,
                                StrSharedEmail = objFile.SharedEmail,
                                Description = objFile.Description,
                                FileSize = objFile.FileSize,
                                FileType = objFile.FileType,
                                SharebleLink = objFile.SharebleLink,
                                LongDescription = objFile.LongDescription,
                                ViewedCount = Convert.ToInt16(objFile.ViewedCount),
                                isReviewed = Convert.ToBoolean(objFile.IsReviewed),
                                UploadedBy = Convert.ToInt16(objFile.UploadedBy),
                                ExpiryDate = Convert.ToDateTime(objFile.ExpiryDate),
                                UploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                                ContentType = objFile.ContentType,
                                FileUserDetails = fileUserDetails,
                                isFavourite = Convert.ToBoolean(objFile.IsFavourite),
                                OwnerEmail = ownerUser.UserEmail,
                                FolderFileCreateDate = Convert.ToDateTime(objFile.UploadedDate),
                                IsFolder = false
                            };
                            lstFiles.Add(obj);
                        }
                    }
                }

                var SharedFolder = _dbContext.FolderToSharedUser.Where(x => x.ToUserEmail == userEmail).ToList();

                if (SharedFolder != null && SharedFolder.Count > 0)
                {
                    foreach (var item in SharedFolder)
                    {
                        var objFolder = fileType == "favourites" ? _dbContext.Folder.Where(x => x.IFolderId == item.IFolderId && x.IsFavourite == true).FirstOrDefault()
                                                                : _dbContext.Folder.Where(x => x.IFolderId == item.IFolderId).FirstOrDefault();

                        if (objFolder != null)
                        {
                            var folderFiles = _dbContext.FileStorage.Where(x => x.IFolderId == objFolder.IFolderId).ToList();
                            Int64 totalFolderSize = 0;
                            int fileCount = 0;
                            foreach (var file in folderFiles)
                            {
                                fileCount++;
                                totalFolderSize = totalFolderSize + Convert.ToInt64(file.FileSize);
                            }
                            var ownerUser = userList.Where(x => x.IUserInfoId == objFolder.IUserId).FirstOrDefault();
                            List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();

                            var sendToEmail = item.ToUserEmail == null ? ownerUser.UserEmail : item.ToUserEmail;
                            var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                            fileUserDetails.Add(new FileUserDetail()
                            {
                                OwnerEmail = ownerUser.UserEmail,
                                SendToEmail = sendToEmail,
                                SendFromEmail = item.FromUserEmail,
                                SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                            });

                            var obj = new FileModel()
                            {
                                iFolderId = Convert.ToInt16(objFolder.IFolderId),
                                FolderName = objFolder.FolderName,
                                FolderFileCreateDate = Convert.ToDateTime(objFolder.FolderCreateDate),
                                FolderCreateDate = Convert.ToDateTime(objFolder.FolderCreateDate),
                                OwnerEmail = ownerUser.UserEmail,
                                IsFolder = true,
                                SharedEmail = new List<string>(),
                                FileUserDetails = fileUserDetails,
                                FileOriginalName = objFolder.FolderName,
                                FolderSize = Convert.ToString(totalFolderSize),
                                FilesCount = fileCount,
                                isFavourite = Convert.ToBoolean(objFolder.IsFavourite)
                            };
                            lstFiles.Add(obj);
                        }
                    }
                }
                res.ResponseData = lstFiles.OrderByDescending(x => x.FolderFileCreateDate).ToList();
               
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public ResponseModel<List<FileModel>> GetFilesByUploadedUserId(int iUserId, string fileType, int iFolderId)
        {
            ResponseModel<List<FileModel>> res = new ResponseModel<List<FileModel>>();
            try
            {
                var lstFiles = new List<FileModel>();

                var allFiles = iFolderId == 0 ? (from f in _dbContext.FileStorage
                                                 where f.IUserInfoId == iUserId && (f.IFolderId == 0 || f.IFolderId == null)
                                                 select f).ToList() :
                                                (from f in _dbContext.FileStorage
                                                 where f.IUserInfoId == iUserId && f.IFolderId == iFolderId
                                                 select f).ToList();
              
                var lstFolder = iFolderId == 0 && (fileType == "0" || fileType == "others" || fileType == "favourites") ?
                                _dbContext.Folder.Where(x => x.IUserId == iUserId).ToList() : new List<Folder>();

                List<FileStorage> files = new List<FileStorage>();
                if (fileType == "0")
                {
                    files = allFiles;
                }
                else if (fileType == "favourites")
                {
                    lstFolder = lstFolder.Where(x => x.IsFavourite == true).ToList();
                    files = allFiles.Where(x => x.IsFavourite == true).ToList();
                }
                else
                {
                    files = allFiles.Where(x => x.FileType == fileType).ToList();
                }

                if (iFolderId > 0)
                {
                    var folder = _dbContext.Folder.FirstOrDefault(x => x.IFolderId == iFolderId);
                    if(folder == null)
                    {
                        res.IsSuccess = true;
                        res.StatusCode = HttpStatusCode.NotFound;
                        res.Message = "Folder not found.";
                        return res;
                    }
                }
                if (iFolderId > 0 && !files.Any())
                {
                    res.IsSuccess = true;
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.Message = "Files not found.";
                }

                var userList = _dbContext.UserInfo.ToList();
                var fileToSharedUser = _dbContext.FileToSharedUser.ToList();
                var folderToSharedUsers = _dbContext.FolderToSharedUser.ToList();

                if (files.Any())
                {
                    foreach (var objFile in files)
                    {
                        var ownerUser = userList.Where(x => x.IUserInfoId == objFile.IUserInfoId).FirstOrDefault();
                        var users = fileToSharedUser.Where(x => x.IFileId == objFile.IFileId).ToList();
                        List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();
                        foreach (var item in users)
                        {
                            var sendToEmail = item.UserEmail == null ? ownerUser.UserEmail : item.UserEmail;
                            var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                            fileUserDetails.Add(new FileUserDetail()
                            {
                                OwnerEmail = ownerUser.UserEmail,
                                SendToEmail = sendToEmail,
                                SendFromEmail = item.FromUserEmail,
                                SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                            });
                        }
                        FileActivity fileActivity = new FileActivity() { 
                            FileUploadedBy = ownerUser.UserEmail,
                            FileUploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                            FileDuplicatedBy = objFile.DuplicateByEmail,
                            FileDuplicateDate = objFile.DuplicateDate,
                            FileReviewedBy = objFile.ReviewedByEmail,
                            FileReviewedDate = objFile.ReviewedDate
                        };
                        var obj = new FileModel()
                        {
                            IFileId = objFile.IFileId,
                            FileOriginalName = objFile.FileOriginalName,
                            AzureFileName = objFile.AzureFileName,
                            FileUrl = objFile.FileUrl,
                            IUserInfoId = Convert.ToInt16(objFile.IUserInfoId),
                            SharedEmail = users.Select(x => x.UserEmail).ToList(),
                            StrSharedEmail = objFile.SharedEmail,
                            Description = objFile.Description,
                            FileSize = objFile.FileSize,
                            FileType = objFile.FileType,
                            SharebleLink = objFile.SharebleLink,
                            LongDescription = objFile.LongDescription,
                            ViewedCount = Convert.ToInt16(objFile.ViewedCount),
                            isReviewed = Convert.ToBoolean(objFile.IsReviewed),
                            UploadedBy = Convert.ToInt16(objFile.UploadedBy),
                            ExpiryDate = Convert.ToDateTime(objFile.ExpiryDate),
                            UploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                            ContentType = objFile.ContentType,
                            FileUserDetails = fileUserDetails,
                            isFavourite = Convert.ToBoolean(objFile.IsFavourite),
                            OwnerEmail = ownerUser.UserEmail,
                            FolderFileCreateDate = Convert.ToDateTime(objFile.UploadedDate),
                            IsFolder = false,
                            fileActivity = fileActivity
                        };
                        lstFiles.Add(obj);
                    }
                }
                if (lstFolder.Any())
                {
                    foreach (var folder in lstFolder)
                    {
                        var folderFiles = _dbContext.FileStorage.Where(x => x.IFolderId == folder.IFolderId).ToList();
                        Int64 totalFolderSize = 0;
                        int fileCount = 0;
                        List<string> fileNames = new List<string>();
                        foreach (var file in folderFiles)
                        {
                            fileCount++;
                            totalFolderSize = totalFolderSize + Convert.ToInt64(file.FileSize);
                            fileNames.Add(file.FileOriginalName);
                        }
                        var ownerUser = userList.Where(x => x.IUserInfoId == folder.IUserId).FirstOrDefault();
                        var users = folderToSharedUsers.Where(x => x.IFolderId == folder.IFolderId).ToList();
                        List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();
                        foreach (var item in users)
                        {
                            var sendToEmail = item.ToUserEmail == null ? ownerUser.UserEmail : item.ToUserEmail;
                            var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                            fileUserDetails.Add(new FileUserDetail()
                            {
                                OwnerEmail = ownerUser.UserEmail,
                                SendToEmail = sendToEmail,
                                SendFromEmail = item.FromUserEmail,
                                SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                            });
                        }
                        FolderActivity folderActivity = new FolderActivity()
                        {
                            FolderCreatedBy = ownerUser.UserEmail,
                            FolderCreatedDate = Convert.ToDateTime(folder.FolderCreateDate),
                            FolderFileNames = fileNames
                        };
                        
                        var obj = new FileModel()
                        {
                            iFolderId = Convert.ToInt16(folder.IFolderId),
                            FolderName = folder.FolderName,
                            FolderFileCreateDate = Convert.ToDateTime(folder.FolderCreateDate),
                            FolderCreateDate = Convert.ToDateTime(folder.FolderCreateDate),
                            OwnerEmail = ownerUser.UserEmail,
                            IsFolder = true,
                            SharedEmail = new List<string>(),
                            FileUserDetails = fileUserDetails,
                            FileOriginalName = folder.FolderName,
                            FolderSize = Convert.ToString(totalFolderSize),
                            FilesCount = fileCount,
                            isFavourite = Convert.ToBoolean(folder.IsFavourite),
                            folderActivity = folderActivity
                        };
                        lstFiles.Add(obj);
                    }
                }

                res.ResponseData = lstFiles.OrderByDescending(x => x.FolderFileCreateDate).ToList();
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public ResponseModel<List<FileModel>> GetSentFilesByUserId(int iUserId)
        {
            ResponseModel<List<FileModel>> res = new ResponseModel<List<FileModel>>();
            try
            {
                var lstFiles = new List<FileModel>();
                var files = (from f in _dbContext.FileStorage
                             join s in _dbContext.FileToSharedUser on f.IFileId equals s.IFileId
                             where f.IUserInfoId == iUserId
                             select f
                                               ).Distinct().OrderByDescending(x => x.UploadedDate).ToList();

                if (files.Count > 0)
                {
                    var userList = _dbContext.UserInfo.ToList();
                    var fileToSharedUser = _dbContext.FileToSharedUser.ToList();

                    foreach (var objFile in files)
                    {
                        var ownerUser = userList.Where(x => x.IUserInfoId == objFile.IUserInfoId).FirstOrDefault();
                        var users = fileToSharedUser.Where(x => x.IFileId == objFile.IFileId).ToList();
                        if (users != null && users.Count > 0)
                        {
                            List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();
                            foreach (var user in users)
                            {
                                var sendToEmail = user.UserEmail == null ? ownerUser.UserEmail : user.UserEmail;
                                var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                                fileUserDetails.Add(new FileUserDetail()
                                {
                                    OwnerEmail = ownerUser.UserEmail,
                                    SendToEmail = sendToEmail,
                                    SendFromEmail = user.FromUserEmail,
                                    SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                    SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                    OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                    OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                                });

                                var obj = new FileModel()
                                {
                                    IFileId = objFile.IFileId,
                                    FileOriginalName = objFile.FileOriginalName,
                                    AzureFileName = objFile.AzureFileName,
                                    FileUrl = objFile.FileUrl,
                                    IUserInfoId = Convert.ToInt16(objFile.IUserInfoId),
                                    StrSharedEmail = user.UserEmail,
                                    Description = objFile.Description,
                                    FileSize = objFile.FileSize,
                                    FileType = objFile.FileType,
                                    SharebleLink = objFile.SharebleLink,
                                    LongDescription = objFile.LongDescription,
                                    ViewedCount = Convert.ToInt16(objFile.ViewedCount),
                                    isReviewed = Convert.ToBoolean(objFile.IsReviewed),
                                    UploadedBy = Convert.ToInt16(objFile.UploadedBy),
                                    ExpiryDate = Convert.ToDateTime(objFile.ExpiryDate),
                                    UploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                                    ContentType = objFile.ContentType,
                                    FileUserDetails = fileUserDetails
                                };
                                lstFiles.Add(obj);
                            }

                        }
                    }

                    res.ResponseData = lstFiles;
                }

                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public async Task<ResponseModel<bool>> DeleteFile(int iFileId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileId).FirstOrDefault();
                if (objFile != null)
                {
                    if (objFile.FileUrl != null && objFile.FileUrl != "")
                    {
                        var fileName = objFile.FileUrl.Replace(m_appSettingsModel.AzureStorage.AzureFileURL.ToString(), "");
                        fileName = fileName.Replace(m_appSettingsModel.AzureStorage.ContainerName.ToString() + "/", "");

                        var _containerName = m_appSettingsModel.AzureStorage.ContainerName.ToString();
                        string _storageConnection = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                        CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_storageConnection);
                        CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
                        CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(_containerName);
                        CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(fileName);
                        //delete blob from container    
                        await _blockBlob.DeleteIfExistsAsync();
                    }

                    var Users = _dbContext.FileToSharedUser.Where(x => x.IFileId == objFile.IFileId).ToList();
                    if (Users.Count > 0)
                    {
                        foreach (var user in Users)
                        {
                            _dbContext.FileToSharedUser.Remove(user);
                        }
                    }
                    _dbContext.FileStorage.Remove(objFile);
                    _dbContext.SaveChanges();
                }

                res.Message = "File deleted successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }
        public ResponseModel<bool> AddSharedUser(AddUserToFileModel reqModel)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if(reqModel.FileId > 0)
                {
                    var objFile = _dbContext.FileStorage.Where(x => x.IFileId == reqModel.FileId)
                                        .FirstOrDefault();
                    var emails = reqModel.ToUserEmails.Split(',');
                    if (emails.Length > 0)
                    {
                        foreach (string email in emails)
                        {
                            var existUser = _dbContext.UserInfo.Where(x => x.UserEmail == email).FirstOrDefault();
                            if (existUser != null)
                            {
                                var obj = new FileToSharedUser()
                                {
                                    IFileId = reqModel.FileId,
                                    UserEmail = email,
                                    UploadedDate = DateTime.Now,
                                    FromUserEmail = reqModel.FromUserEmail
                                };
                                _dbContext.FileToSharedUser.Add(obj);

                                sendEmail(existUser, EnumEmailType.fileShare, Convert.ToInt32(objFile.IUserInfoId), objFile.FileOriginalName, FormatSize(objFile.FileSize), FormatDate(Convert.ToDateTime(objFile.ExpiryDate)));
                            }
                            else
                            {
                                UserInfo user = new UserInfo()
                                {
                                    FirstName = "",
                                    LastName = "",
                                    UserEmail = email,
                                    TempName = "Guest User",
                                    CreatedDate = DateTime.Now
                                };
                                _dbContext.UserInfo.Add(user);
                                _dbContext.SaveChanges();

                                var token = generateToken(user.UserEmail, Convert.ToInt16(user.IUserInfoId));
                                user.Token = token;
                                _dbContext.UserInfo.Update(user);
                                _dbContext.SaveChanges();
                                _commonLibrary.sendMail(user.UserEmail, EnumEmailType.registerUser, token, user.TempName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                                sendEmail(user, EnumEmailType.fileShare, Convert.ToInt32(objFile.IUserInfoId), objFile.FileOriginalName, FormatSize(objFile.FileSize), FormatDate(Convert.ToDateTime(objFile.ExpiryDate)));
                            }


                        }
                    }

                    _dbContext.SaveChanges();
                    res.Message = "File shared successfully.";
                }
                else if(reqModel.iFolderId > 0)
                {
                    var folderUserId = Convert.ToInt16(_dbContext.Folder.Where(x => x.IFolderId == reqModel.iFolderId)
                                        .Select(x => x.IUserId).FirstOrDefault());
                    var emails = reqModel.ToUserEmails.Split(',');
                    if (emails.Length > 0)
                    {
                        foreach (string email in emails)
                        {
                            var existUser = _dbContext.UserInfo.Where(x => x.UserEmail == email).FirstOrDefault();
                            if (existUser != null)
                            {
                                var obj = new FolderToSharedUser()
                                {
                                    IFolderId = reqModel.iFolderId,
                                    ToUserEmail = email,
                                    UploadedDate = DateTime.Now,
                                    FromUserEmail = reqModel.FromUserEmail
                                };
                                _dbContext.FolderToSharedUser.Add(obj);
                                var files = _dbContext.FileStorage.Where(x => x.IFolderId == reqModel.iFolderId).ToList();
                                var fileCount = 0;
                                Int64 fileSize = 0;
                                if (files.Any())
                                {
                                    foreach(var f in files)
                                    {
                                        fileSize = fileSize + Convert.ToInt64(f.FileSize);
                                        fileCount++;
                                    }
                                }
                                sendEmail(existUser, EnumEmailType.folderShare, folderUserId, "", FormatSize(Convert.ToString(fileSize)), Convert.ToString(fileCount));
                            }
                            else
                            {
                                UserInfo user = new UserInfo()
                                {
                                    FirstName = "",
                                    LastName = "",
                                    UserEmail = email,
                                    TempName = "Guest User",
                                    CreatedDate = DateTime.Now
                                };
                                _dbContext.UserInfo.Add(user);
                                _dbContext.SaveChanges();

                                var token = generateToken(user.UserEmail, Convert.ToInt16(user.IUserInfoId));
                                user.Token = token;
                                _dbContext.UserInfo.Update(user);
                                _dbContext.SaveChanges();
                                _commonLibrary.sendMail(user.UserEmail, EnumEmailType.registerUser, token, user.TempName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));

                                var files = _dbContext.FileStorage.Where(x => x.IFolderId == reqModel.iFolderId).ToList();
                                var fileCount = 0;
                                Int64 fileSize = 0;
                                if (files.Any())
                                {
                                    foreach (var f in files)
                                    {
                                        fileSize = fileSize + Convert.ToInt64(f.FileSize);
                                        fileCount++;
                                    }
                                }

                                sendEmail(user, EnumEmailType.folderShare, folderUserId, "", FormatSize(Convert.ToString(fileSize)), Convert.ToString(fileCount));
                            }
                        }
                    }

                    _dbContext.SaveChanges();
                    res.Message = "Folder shared successfully.";
                }
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<bool> RemoveSharedUser(int iFileId, int iFolderId, string userEmails)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if(iFileId > 0)
                {
                    var emails = userEmails.Split(',');
                    if (emails.Length > 0)
                    {
                        foreach (string email in emails)
                        {
                            var sharedUser = _dbContext.FileToSharedUser.Where(x => x.IFileId == iFileId && x.UserEmail == email).FirstOrDefault();
                            if(sharedUser != null)
                            {
                                _dbContext.FileToSharedUser.Remove(sharedUser);
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                }
               else if(iFolderId > 0)
                {
                    var emails = userEmails.Split(',');
                    if (emails.Length > 0)
                    {
                        foreach (string email in emails)
                        {
                            var sharedUser = _dbContext.FolderToSharedUser.Where(x => x.IFolderId == iFolderId && x.ToUserEmail == email).FirstOrDefault();
                            if(sharedUser != null)
                            {
                                _dbContext.FolderToSharedUser.Remove(sharedUser);
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                }

                res.Message = "User removed successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }
        public ResponseModel<bool> IncrementViewCount(int iFileId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileId).FirstOrDefault();
                if (objFile != null)
                {
                    objFile.ViewedCount = objFile.ViewedCount + 1;
                    _dbContext.FileStorage.Update(objFile);
                    _dbContext.SaveChanges();
                }

                res.Message = "View count updated successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<bool> SendEmail(EmailReqModel reqModel)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if (reqModel.EnumEmailFor == EnumEmailType.viewVideo.ToString())
                {
                    _commonLibrary.sendMail(reqModel.ToEmail, EnumEmailType.viewVideo, reqModel.Token, reqModel.FromUserName, reqModel.FromUserId, reqModel.ToUserId);
                }
                else if (reqModel.EnumEmailFor == EnumEmailType.reviewContent.ToString())
                {
                    _commonLibrary.sendMail(reqModel.ToEmail, EnumEmailType.reviewContent, reqModel.Token, reqModel.FromUserName, reqModel.FromUserId, reqModel.ToUserId);
                }
                else if (reqModel.EnumEmailFor == EnumEmailType.downloadContent.ToString())
                {
                    _commonLibrary.sendMail(reqModel.ToEmail, EnumEmailType.downloadContent, reqModel.Token, reqModel.FromUserName, reqModel.FromUserId, reqModel.ToUserId);
                }
                else if (reqModel.EnumEmailFor == EnumEmailType.loginUser.ToString())
                {
                    _commonLibrary.sendMail(reqModel.ToEmail, EnumEmailType.loginUser, reqModel.Token, reqModel.FromUserName, reqModel.FromUserId, reqModel.ToUserId);
                }

                res.Message = "Email sent successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public ResponseModel<bool> AddRemoveFavourites(int iFileFolderId, bool isFavourite, bool isFolder)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if (isFolder)
                {
                    var objFolder = _dbContext.Folder.Where(x => x.IFolderId == iFileFolderId).FirstOrDefault();
                    if(objFolder == null)
                    {
                        res.Message = "Folder not found.";
                        res.IsSuccess = false;
                        res.StatusCode = HttpStatusCode.NotFound;
                        return res;
                    }
                    objFolder.IsFavourite = isFavourite;
                    _dbContext.Folder.Update(objFolder);
                    _dbContext.SaveChanges();
                    res.Message = isFavourite ? "Folder added to favourites successfully." : "Folder removed from favourites successfully.";
                }
                else
                {
                    var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileFolderId).FirstOrDefault();
                    if (objFile == null)
                    {
                        res.Message = "File not found.";
                        res.IsSuccess = false;
                        res.StatusCode = HttpStatusCode.NotFound;
                        return res;
                    }
                    objFile.IsFavourite = isFavourite;
                    _dbContext.FileStorage.Update(objFile);
                    _dbContext.SaveChanges();
                    res.Message = isFavourite ? "File added to favourites successfully." : "File removed from favourites successfully.";
                }

                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<bool> RenameFileFolder(int iFileFolderId, string newName, bool isFolder)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if (isFolder)
                {
                    var objFolder = _dbContext.Folder.Where(x => x.IFolderId == iFileFolderId).FirstOrDefault();
                    objFolder.FolderName = newName;
                    _dbContext.Folder.Update(objFolder);
                    res.Message = "Folder renamed seccessfully.";
                }
                else
                {
                    var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileFolderId).FirstOrDefault();
                    objFile.FileOriginalName = newName;
                    _dbContext.FileStorage.Update(objFile);
                    res.Message = "File renamed seccessfully.";
                }
                _dbContext.SaveChanges();
               
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<int> CreateFolder(FolderDetail reqModel)
        {
            ResponseModel<int> res = new ResponseModel<int>();
            try
            {
                var folder = _dbContext.Folder.Where(x => x.IUserId == reqModel.iUserInfoId).ToList();
                foreach (var item in folder)
                {
                    if (item.FolderName == reqModel.FolderName)
                    {
                        res.Message = "Folder name already exist.";
                        res.ResponseData = 0;
                        res.StatusCode = HttpStatusCode.Ambiguous;
                        res.IsSuccess = false;
                        return res;
                    }
                }
                Folder obj = new Folder() { FolderName = reqModel.FolderName, IUserId = reqModel.iUserInfoId, FolderCreateDate = DateTime.Now };
                _dbContext.Folder.Add(obj);
                _dbContext.SaveChanges();

                res.Message = "Folder created successfully.";
                res.ResponseData = Convert.ToInt16(obj.IFolderId);
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = 0;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }
        public ResponseModel<bool> MoveToFolder(int iFolderId, int iFileId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objFile = _dbContext.FileStorage.Where(x => x.IFileId == iFileId).FirstOrDefault();
                var objFolder = _dbContext.Folder.FirstOrDefault(x => x.IFolderId == iFolderId);
                if (objFile != null && (objFolder != null || iFolderId == 0))
                {
                    objFile.IFolderId = iFolderId;
                    _dbContext.FileStorage.Update(objFile);
                    _dbContext.SaveChanges();
                    res.Message = "File moved to folder successfully.";
                }
                else
                {
                    res.Message = "File/Folder not found.";
                }
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public ResponseModel<List<FolderDetail>> GetFoldersByUserId(int iUserInfoId)
        {
            ResponseModel<List<FolderDetail>> res = new ResponseModel<List<FolderDetail>>();
            try
            {
                res.ResponseData = new List<FolderDetail>();
                var objFolder = _dbContext.Folder.Where(x => x.IUserId == iUserInfoId).OrderBy(x => x.FolderCreateDate).ToList();
                foreach (var item in objFolder)
                {
                    FolderDetail obj = new FolderDetail() { iFolderId = Convert.ToInt16(item.IFolderId), FolderName = item.FolderName };
                    res.ResponseData.Add(obj);
                }
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = new List<FolderDetail>();
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public async Task<ResponseModel<bool>> DeleteFolder(int iFolderId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var folder = _dbContext.Folder.Where(x => x.IFolderId == iFolderId).FirstOrDefault();
                var lstFiles = _dbContext.FileStorage.Where(x => x.IFolderId == iFolderId).ToList();
                foreach (var item in lstFiles)
                {
                    await DeleteFile(Convert.ToInt16(item.IFileId));
                }

                _dbContext.Folder.Remove(folder);
                _dbContext.SaveChanges();

                res.Message = "Folder deleted successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<FolderWithFileModel> FetFolderDetailWithFiles(int iFolderId)
        {
            ResponseModel<FolderWithFileModel> res = new ResponseModel<FolderWithFileModel>();
            try
            {
                var folderDetail = new FolderWithFileModel();

                var files = (from f in _dbContext.FileStorage
                             where f.IFolderId == iFolderId
                             orderby f.UploadedDate descending
                             select f).ToList();
                var objFolder = _dbContext.Folder.Where(x => x.IFolderId == iFolderId).FirstOrDefault();

                if (objFolder == null)
                {
                    res.IsSuccess = true;
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.Message = "Folder not found.";
                    return res;
                }

                var userList = _dbContext.UserInfo.ToList();
                var fileToSharedUser = _dbContext.FileToSharedUser.ToList();

                folderDetail.iFolderId = Convert.ToInt16(objFolder.IFolderId);
                folderDetail.FolderName = objFolder.FolderName;
                folderDetail.FolderSize = "";
                folderDetail.Files = new List<FileModel>();
                if (files.Any())
                {
                    Int64 totalFolderSize = 0;
                    int fileCount = 0;

                    foreach (var objFile in files)
                    {
                        var ownerUser = userList.Where(x => x.IUserInfoId == objFile.IUserInfoId).FirstOrDefault();
                        var users = fileToSharedUser.Where(x => x.IFileId == objFile.IFileId).ToList();
                        List<FileUserDetail> fileUserDetails = new List<FileUserDetail>();
                        foreach (var item in users)
                        {
                            var sendToEmail = item.UserEmail == null ? ownerUser.UserEmail : item.UserEmail;
                            var userDetail = userList.Where(x => x.UserEmail == sendToEmail).FirstOrDefault();
                            fileUserDetails.Add(new FileUserDetail()
                            {
                                OwnerEmail = ownerUser.UserEmail,
                                SendToEmail = sendToEmail,
                                SendFromEmail = item.FromUserEmail,
                                SendToUserName = userDetail != null ? userDetail.FirstName != "" ? userDetail.FirstName + " " + userDetail.LastName : userDetail.TempName : "",
                                SendToUserProfilePic = userDetail != null && userDetail.ProfilePicUrl != null && userDetail.ProfilePicUrl != "" ? userDetail.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/02_Ogre.png",
                                OwnerUserName = ownerUser != null ? ownerUser.FirstName != "" ? ownerUser.FirstName + " " + ownerUser.LastName : ownerUser.TempName : "",
                                OwnerUserProfilePic = ownerUser != null && ownerUser.ProfilePicUrl != null && ownerUser.ProfilePicUrl != "" ? ownerUser.ProfilePicUrl : "https://prevueit.blob.core.windows.net/prevueit-template-images/16_Fairy.png"
                            });
                        }
                        FileActivity fileActivity = new FileActivity()
                        {
                            FileUploadedBy = ownerUser.UserEmail,
                            FileUploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                            FileDuplicatedBy = objFile.DuplicateByEmail,
                            FileDuplicateDate = objFile.DuplicateDate,
                            FileReviewedBy = objFile.ReviewedByEmail,
                            FileReviewedDate = objFile.ReviewedDate
                        };
                        var obj = new FileModel()
                        {
                            IFileId = objFile.IFileId,
                            FileOriginalName = objFile.FileOriginalName,
                            AzureFileName = objFile.AzureFileName,
                            FileUrl = objFile.FileUrl,
                            IUserInfoId = Convert.ToInt16(objFile.IUserInfoId),
                            SharedEmail = users.Select(x => x.UserEmail).ToList(),
                            StrSharedEmail = objFile.SharedEmail,
                            Description = objFile.Description,
                            FileSize = objFile.FileSize,
                            FileType = objFile.FileType,
                            SharebleLink = objFile.SharebleLink,
                            LongDescription = objFile.LongDescription,
                            ViewedCount = Convert.ToInt16(objFile.ViewedCount),
                            isReviewed = Convert.ToBoolean(objFile.IsReviewed),
                            UploadedBy = Convert.ToInt16(objFile.UploadedBy),
                            ExpiryDate = Convert.ToDateTime(objFile.ExpiryDate),
                            UploadedDate = Convert.ToDateTime(objFile.UploadedDate),
                            ContentType = objFile.ContentType,
                            FileUserDetails = fileUserDetails,
                            isFavourite = Convert.ToBoolean(objFile.IsFavourite),
                            OwnerEmail = ownerUser.UserEmail,
                            FolderFileCreateDate = Convert.ToDateTime(objFile.UploadedDate),
                            IsFolder = false,
                            fileActivity = fileActivity
                        };
                        totalFolderSize = totalFolderSize + Convert.ToInt64(objFile.FileSize);
                        fileCount++;
                        folderDetail.Files.Add(obj);
                    }


                    folderDetail.FolderSize = Convert.ToString(totalFolderSize);
                    folderDetail.FileCount = fileCount;
                }

                res.ResponseData = folderDetail;
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public async Task<ResponseModel<bool>> DuplicateFile(int iFileId, int iUserId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var fileModel = _dbContext.FileStorage.FirstOrDefault(x => x.IFileId == iFileId);
                var userDetail = _dbContext.UserInfo.FirstOrDefault(x => x.IUserInfoId == iUserId);
                //Azure copy
                string newFileURL = await CopyFileInAzure(fileModel.FileUrl, fileModel.FileOriginalName);

                var objFile = new FileStorage()
                {
                    FileOriginalName = "Copy of " + fileModel.FileOriginalName,
                    FileUrl = newFileURL,
                    IUserInfoId = iUserId,
                    SharedEmail = "",
                    Description = fileModel.Description,
                    FileSize = fileModel.FileSize,
                    SharebleLink = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    LongDescription = fileModel.LongDescription,
                    ViewedCount = 0,
                    UploadedBy = iUserId,
                    UploadedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddMonths(1),
                    FileType = fileModel.FileType,
                    ContentType = fileModel.ContentType,
                    AzureFileName = "",
                    IFolderId = fileModel.IFolderId
                };

                _dbContext.FileStorage.Add(objFile);

                fileModel.DuplicateByEmail = userDetail.UserEmail;
                fileModel.DuplicateDate = DateTime.Now;
                _dbContext.FileStorage.Update(fileModel);

                _dbContext.SaveChanges();

                var fileId = objFile.IFileId;
               
                ////check storage for user
                //long usedByes = 0;
                //long gb = 1024 * 1024 * 1024;
                //long remainingBytes = 5 * gb;
                //long limitBytes = 4 * gb;
                //var files = _dbContext.FileStorage.Where(x => x.IUserInfoId == iUserId).ToList();
                //if (files != null)
                //{
                //    foreach (var file in files)
                //    {
                //        usedByes = Convert.ToInt64(usedByes + Convert.ToInt64(file.FileSize));
                //    }
                //}
                //remainingBytes = remainingBytes - usedByes;
                //if (remainingBytes <= limitBytes)
                //{
                //    var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == iUserId).FirstOrDefault();
                //    string userName = user != null ? user.FirstName != null ? user.FirstName + " " + user.LastName : user.TempName : "";
                //    _commonLibrary.sendMail(user.UserEmail, EnumEmailType.storageLimit, "", userName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                //}

                res.IsSuccess = true;
                res.Message = "Copy of file created successfully.";
                res.ResponseData = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = false;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }
        public ResponseModel<List<SearchFiles>> GetSearchFileLists(string fileType, int iUserId, string userEmail)
        {
            ResponseModel<List<SearchFiles>> res = new ResponseModel<List<SearchFiles>>();
            try
            {
                var allFiles = _dbContext.FileStorage.Where(x => x.IUserInfoId == iUserId).ToList();
                var files = new List<FileStorage>();
                if (fileType == "0")
                {
                    files = allFiles;
                }
                else if (fileType == "favourites")
                {
                    files = allFiles.Where(x => x.IsFavourite == true).ToList();
                }
                else if(fileType =="received")
                {
                    var folderIds = _dbContext.FolderToSharedUser.Where(x => x.ToUserEmail == userEmail).Select(x => x.IFolderId).ToList();
                    var filesIds = _dbContext.FileToSharedUser.Where(x => x.UserEmail == userEmail).Select(x=> x.IFileId).Distinct().ToList();
                    files = _dbContext.FileStorage.Where(x => filesIds.Contains(x.IFileId) || folderIds.Contains(x.IFolderId)).ToList();
                }

                var folders = _dbContext.Folder.ToList();
                res.ResponseData = new List<SearchFiles>();
                if (files.Any())
                {
                    foreach(var file in files)
                    {
                        var folder = folders.FirstOrDefault(x => x.IFolderId == file.IFolderId);
                        SearchFiles f = new SearchFiles()
                        {
                            FileOriginalName = file.FileOriginalName,
                            iFileId = Convert.ToInt16(file.IFileId),
                            iFolderId = Convert.ToInt16(file.IFolderId),
                            FolderName = folder != null ? folder.FolderName : "Dashboard",
                            FileUploadedDate = Convert.ToDateTime(file.UploadedDate)
                        };
                        res.ResponseData.Add(f);
                    }
                }
                res.IsSuccess = true;
            }
            catch(Exception ex)
            {
                res.ResponseData = new List<SearchFiles>();
                res.IsSuccess=false;
                res.Message = ex.Message;
            }
            return res;
        }

        public async Task<string> CopyFileInAzure(string originalURL, string originalFileName)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString());
            
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(originalURL));
            var objName = Convert.ToString(originalFileName).Split('.');
            string newFileName = "File" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + objName[1];
            
            string storageAccountConnectionString = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
            string destinationContainer = m_appSettingsModel.AzureStorage.ContainerName.ToString();
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();

            var blobContainer = blobClient.GetContainerReference(destinationContainer);
            await blobContainer.CreateIfNotExistsAsync();
           
            var newBlockBlob = blobContainer.GetBlockBlobReference(newFileName);
            await newBlockBlob.StartCopyAsync(blob);


            string returnURL = m_appSettingsModel.AzureStorage.AzureFileURL.ToString() + m_appSettingsModel.AzureStorage.ContainerName.ToString() + "/" + newFileName + "?" + m_appSettingsModel.AzureStorage.AzureSASToken.ToString();
            return returnURL;
        }
        public string generateToken(string email, int userId)
        {
            string access_token = "";
            try
            {
                var strSecretKey = m_appSettingsModel.Jwt.Key;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(strSecretKey);
                var expiry = DateTime.Now.AddDays(15);

                var iUserInfoId = userId;
                var strEnumRole = "User";
                var strEmailAddress = email;
                var strLoginDisplayName = "UserName";
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ApplicationConstants.UserId, iUserInfoId.ToString()),
                        new Claim(ApplicationConstants.UserName, strLoginDisplayName),
                        new Claim(ApplicationConstants.UserRole, strEnumRole),
                        new Claim(ApplicationConstants.UserEmail, email)
                    }),
                    Expires = expiry,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = m_appSettingsModel.Jwt.Issuer,
                    IssuedAt = DateTime.Now,

                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                access_token = tokenHandler.WriteToken(token);

            }
            catch (Exception ex)
            {
                //throw ex;
            }

            return access_token;
        }

        public void sendEmail(UserInfo existUser, EnumEmailType enumEmailType, int fromUserId, string fileName = "", string fileSize = "", string fileExpiryDate = "")
        {
            var token = generateToken(existUser.UserEmail, Convert.ToInt16(existUser.IUserInfoId));
            existUser.Token = token;
            _dbContext.UserInfo.Update(existUser);
            _dbContext.SaveChanges();

            var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == fromUserId).FirstOrDefault();
            var userName = user.FirstName != null && user.FirstName != "" ? user.FirstName + " " + user.LastName : user.TempName;
            if(enumEmailType == EnumEmailType.fileShare || enumEmailType == EnumEmailType.folderShare)
            {
                userName = user.UserEmail;
            }
            _commonLibrary.sendMail(existUser.UserEmail, enumEmailType, token, userName, fromUserId, Convert.ToInt16(existUser.IUserInfoId), fileName, fileSize, fileExpiryDate);

        }

        public static string FormatSize(string strBytes)
        {
            Int64.TryParse(strBytes, out Int64 bytes);
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
        public string FormatDate(DateTime date)
        {
            return date.ToString("d MMMM, yyyy",CultureInfo.CreateSpecificCulture("en-US"));
        }
    }

}

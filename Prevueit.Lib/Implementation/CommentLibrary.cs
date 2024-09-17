using Prevueit.Db.Models;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using static Prevueit.Lib.Enum;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Prevueit.Lib.Implementation
{
    public class CommentLibrary : ICommentLibrary
    {

        #region Variable Declartion
        private readonly prevuitContext _dbContext;
        private readonly AppSettingsModel m_appSettingsModel;
        private readonly ICommonLibrary _commonLibrary;
        #endregion

        #region constructor
        public CommentLibrary(prevuitContext dbContext, AppSettingsModel appSettingsModel, ICommonLibrary commonLibrary)
        {
            _dbContext = dbContext;
            m_appSettingsModel = appSettingsModel;
            _commonLibrary = commonLibrary;
        }
        #endregion

        public async Task<ResponseModel<string>> UploadComment(dynamic files)
        {
            ResponseModel<string> res = new ResponseModel<string>();
            try
            {
                string path = "";
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                            var objName = Convert.ToString(fileName).Split(".");
                            fileName = "Comment" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + objName[1];
                            string storageAccountConnectionString = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                            string containerName = "prevueit-comment";
                            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
                            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);
                            await Container.CreateIfNotExistsAsync();
                            CloudBlockBlob blob = Container.GetBlockBlobReference(fileName);
                            HashSet<string> blocklist = new HashSet<string>();
                            path = containerName +"/" + fileName;
                            const int pageSizeInBytes = 10485760;
                            long prevLastByte = 0;
                            long bytesRemain = file.Length;

                            byte[] bytes;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var fileStream = file.OpenReadStream();
                                await fileStream.CopyToAsync(ms);
                                bytes = ms.ToArray();
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
                                blob.Properties.ContentType = file.ContentType;
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
                res.Message = "File uploaded successfully.";
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
        public ResponseModel<bool> AddUpdateComment(Comment objComment)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                if (objComment.ICommentId > 0)
                {
                    objComment.CommentUpdateDate = DateTime.Now;
                    _dbContext.Comment.Add(objComment);

                    res.Message = "Comment updated successfully.";
                }
                else
                {
                    objComment.CommentCreateDate = DateTime.Now;
                    _dbContext.Comment.Add(objComment);

                    var files = _dbContext.FileToSharedUser.Where(x => x.IFileId == objComment.IFileId).ToList();
                    var fileOwner = _dbContext.FileStorage.Where(x => x.IFileId == objComment.IFileId).FirstOrDefault();
                    var commentFrom = _dbContext.UserInfo.Where(x => x.IUserInfoId == objComment.IUserId).FirstOrDefault();
                    var fromUserName = commentFrom != null ?
                                        commentFrom.FirstName != null ?
                                        commentFrom.FirstName + " " + commentFrom.LastName :
                                       commentFrom.TempName : "";
                    if(fileOwner != null)
                    {
                        var toUser = _dbContext.UserInfo.Where(x => x.IUserInfoId == fileOwner.IUserInfoId).FirstOrDefault();
                        if (toUser != null)
                        {
                            _commonLibrary.sendMail(toUser.UserEmail, EnumEmailType.addComment, "", fromUserName, Convert.ToInt32(commentFrom.IUserInfoId), Convert.ToInt32(fileOwner.IUserInfoId), fileOwner.FileOriginalName, "", "",Convert.ToInt16(fileOwner.IFileId));
                        }
                    }
                    if (files.Count > 0)
                    {
                        foreach(var file in files)
                        {
                            var toUser = _dbContext.UserInfo.Where(x => x.UserEmail == file.UserEmail).FirstOrDefault();
                            if(toUser != null)
                            {
                                _commonLibrary.sendMail(toUser.UserEmail, EnumEmailType.addComment, "", fromUserName, Convert.ToInt32(commentFrom.IUserInfoId), Convert.ToInt32(toUser.IUserInfoId), fileOwner.FileOriginalName, "", "", Convert.ToInt16(fileOwner.IFileId));
                            }
                        }
                    }

                    res.Message = "Comment added successfully.";
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

        public async Task< ResponseModel<bool>> RemoveComment(int iCommentId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var objComment = _dbContext.Comment.Where(x => x.ICommentId == iCommentId).FirstOrDefault();
                if (objComment != null)
                {
                    if(objComment.CommentUrl != null && objComment.CommentUrl != "")
                    {
                        var fileName = objComment.CommentUrl.Replace(m_appSettingsModel.AzureStorage.AzureFileURL.ToString(), "");
                        fileName = fileName.Replace(m_appSettingsModel.AzureStorage.ContainerName.ToString() + "/", "");

                        var _containerName = "prevueit-comment";
                        string _storageConnection = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                        CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_storageConnection);
                        CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
                        CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(_containerName);
                        CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(fileName);
                        //delete blob from container    
                        await _blockBlob.DeleteIfExistsAsync();
                    }
                    _dbContext.Comment.Remove(objComment);
                    _dbContext.SaveChanges();
                }

                res.Message = "Comment removed successfully.";
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

        public ResponseModel<List<CommentModel>> GetCommentsByFileId(int iFileId)
        {
            ResponseModel<List<CommentModel>> res = new ResponseModel<List<CommentModel>>();
            try
            {
                res.ResponseData = new List<CommentModel>();
                var lstComment = _dbContext.Comment.Where(x => x.IFileId == iFileId).ToList();
                if (lstComment.Count > 0)
                {
                    foreach (var comment in lstComment)
                    {
                        var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == comment.IUserId).FirstOrDefault();
                        var objComment = new CommentModel()
                        {
                            ICommentId = Convert.ToInt16(comment.ICommentId),
                            IFileId =  Convert.ToInt16(comment.IFileId),
                            IUserId = Convert.ToInt16(comment.IUserId),
                            UserName = user != null ? user.FirstName != "" && user.FirstName != null ? user.FirstName + " " + user.LastName : user.TempName : "",
                            UserEmail = user != null ? user.UserEmail : "",
                            CommentText = comment.CommentText,
                            CommentType = comment.CommentType,
                            CommentUrl = comment.CommentUrl,
                            IParentCommentId = Convert.ToInt16(comment.IParentCommentId),
                            VideoFrametime = comment.VideoFrametime,
                            LikeCount = Convert.ToInt16(comment.LikeCount),
                            CommentCreateDate = Convert.ToDateTime(comment.CommentCreateDate),
                            CommentUpdateDate = Convert.ToDateTime(comment.CommentUpdateDate),
                        };
                        res.ResponseData.Add(objComment);
                    }
                }

                res.StatusCode = HttpStatusCode.OK;
                res.IsSuccess = true;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public ResponseModel<int> LikeComment(int iCommentId)
        {
            ResponseModel<int> res = new ResponseModel<int>();
            try
            {
                var objComment = _dbContext.Comment.Where(x => x.ICommentId == iCommentId).FirstOrDefault();
                if (objComment != null)
                {
                    objComment.LikeCount = objComment.LikeCount + 1;
                    _dbContext.Comment.Update(objComment);
                    _dbContext.SaveChanges();

                    res.ResponseData =Convert.ToInt16(objComment.LikeCount);
                }

                res.Message = "Comment liked successfully.";
                
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
    }
}
